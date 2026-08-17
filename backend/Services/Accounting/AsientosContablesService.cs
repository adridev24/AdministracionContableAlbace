using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Accounting;
using BudgetControl.Api.Models.Accounting;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Accounting
{
    public class AsientosContablesService : IAsientosContablesService
    {
        private const string ModuloContabilidad = "Contabilidad";
        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;

        public AsientosContablesService(AppDbContext db, IUserContext userContext)
        {
            _db = db;
            _userContext = userContext;
        }

        public async Task<IEnumerable<AsientoContableListResponse>> GetAsientosAsync(AsientoContableFilter filter)
        {
            var query = _db.AsientosContables
                .AsNoTracking()
                .Include(a => a.Detalles)
                .AsQueryable();

            if (filter.FechaDesde.HasValue)
            {
                query = query.Where(a => a.Fecha >= EnsureUtc(filter.FechaDesde.Value.Date));
            }

            if (filter.FechaHasta.HasValue)
            {
                query = query.Where(a => a.Fecha <= EnsureUtc(filter.FechaHasta.Value.Date.AddDays(1).AddTicks(-1)));
            }

            var descripcion = filter.Descripcion?.Trim();
            if (!string.IsNullOrWhiteSpace(descripcion))
            {
                query = query.Where(a => a.Descripcion.ToLower().Contains(descripcion.ToLower()));
            }

            if (filter.CuentaContableId.HasValue)
            {
                query = query.Where(a => a.Detalles.Any(d => d.CuentaContableId == filter.CuentaContableId.Value));
            }

            var tipo = filter.TipoAsiento?.Trim().ToLowerInvariant();
            query = tipo switch
            {
                "manual" => query.Where(a => !a.EsAutomatico && !a.EsReversion),
                "automatico" => query.Where(a => a.EsAutomatico && !a.EsReversion),
                "reversion" => query.Where(a => a.EsReversion),
                _ => query
            };

            var reversionsByOriginal = await _db.AsientosContables
                .AsNoTracking()
                .Where(a => a.IdAsientoRevertido.HasValue)
                .Select(a => new { OriginalId = a.IdAsientoRevertido!.Value, ReversionId = a.Id })
                .ToListAsync();

            var reversionMap = reversionsByOriginal
                .GroupBy(r => r.OriginalId)
                .ToDictionary(g => g.Key, g => (int?)g.OrderBy(r => r.ReversionId).First().ReversionId);

            var asientos = await query
                .OrderByDescending(a => a.Fecha)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var estado = filter.Estado?.Trim().ToLowerInvariant();
            var responses = asientos.Select(a => MapList(a, reversionMap.GetValueOrDefault(a.Id))).ToList();

            return estado switch
            {
                "normal" => responses.Where(a => a.Estado == "Normal").ToList(),
                "reversado" => responses.Where(a => a.Estado == "Reversado").ToList(),
                _ => responses
            };
        }

        public async Task<AsientoContableResponse?> GetAsientoAsync(int id)
        {
            var asiento = await GetAsientoDetalleQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (asiento == null)
            {
                return null;
            }

            var reversionId = await GetReversionIdAsync(id);
            return MapDetalle(asiento, reversionId);
        }

        public Task<AsientoContableResponse> CrearAsientoManualAsync(CrearAsientoContableRequest request)
        {
            return CrearAsientoAsync(
                fecha: request.Fecha,
                descripcion: request.Descripcion,
                moduloOrigen: null,
                idOrigen: null,
                esAutomatico: false,
                esReversion: false,
                idAsientoRevertido: null,
                detalles: request.Detalles);
        }

        public async Task<AsientoContableResponse> ReversarAsientoAsync(int id)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var asientoOriginal = await GetAsientoDetalleQuery()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (asientoOriginal == null)
            {
                throw new KeyNotFoundException("Asiento contable no encontrado.");
            }

            if (asientoOriginal.EsReversion)
            {
                throw new InvalidOperationException("No se puede reversar un asiento de reversion.");
            }

            if (await _db.AsientosContables.AnyAsync(a => a.IdAsientoRevertido == id))
            {
                throw new InvalidOperationException("El asiento ya fue reversado.");
            }

            var detalles = asientoOriginal.Detalles
                .OrderBy(d => d.Id)
                .Select(d => new CrearAsientoContableDetalleRequest
                {
                    CuentaContableId = d.CuentaContableId,
                    Descripcion = $"Reversion: {d.Descripcion}",
                    Debe = d.Haber,
                    Haber = d.Debe
                })
                .ToList();

            var asientoReversion = BuildAsiento(
                fecha: DateTime.UtcNow,
                descripcion: $"Reversion asiento #{asientoOriginal.Id}: {asientoOriginal.Descripcion}",
                moduloOrigen: ModuloContabilidad,
                idOrigen: asientoOriginal.Id.ToString(),
                esAutomatico: true,
                esReversion: true,
                idAsientoRevertido: asientoOriginal.Id,
                detalles: detalles,
                usuarioAlta: _userContext.UserName);

            await ValidateDetallesAsync(detalles, requireActiveAccounts: false);
            ValidateBalance(detalles);

            _db.AsientosContables.Add(asientoReversion);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            var saved = await GetAsientoAsync(asientoReversion.Id);
            return saved!;
        }

        public Task<AsientoContableResponse> GenerarAsientoAutomaticoAsync(
            string moduloOrigen,
            string idOrigen,
            DateTime fecha,
            string descripcion,
            IEnumerable<CrearAsientoContableDetalleRequest> detalles)
        {
            return CrearAsientoAsync(
                fecha,
                descripcion,
                moduloOrigen,
                idOrigen,
                esAutomatico: true,
                esReversion: false,
                idAsientoRevertido: null,
                detalles: detalles.ToList());
        }

        private async Task<AsientoContableResponse> CrearAsientoAsync(
            DateTime fecha,
            string descripcion,
            string? moduloOrigen,
            string? idOrigen,
            bool esAutomatico,
            bool esReversion,
            int? idAsientoRevertido,
            List<CrearAsientoContableDetalleRequest> detalles)
        {
            var ownsTransaction = _db.Database.CurrentTransaction == null;
            var transaction = ownsTransaction ? await _db.Database.BeginTransactionAsync() : null;
            try
            {
                var normalizedDescripcion = NormalizeRequired(descripcion, "La descripcion del asiento es obligatoria.");
                await ValidateDetallesAsync(detalles, requireActiveAccounts: true);
                ValidateBalance(detalles);

                var asiento = BuildAsiento(
                    fecha,
                    normalizedDescripcion,
                    moduloOrigen,
                    idOrigen,
                    esAutomatico,
                    esReversion,
                    idAsientoRevertido,
                    detalles,
                    _userContext.UserName);

                _db.AsientosContables.Add(asiento);
                await _db.SaveChangesAsync();
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                var saved = await GetAsientoAsync(asiento.Id);
                return saved!;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        private AsientoContable BuildAsiento(
            DateTime fecha,
            string descripcion,
            string? moduloOrigen,
            string? idOrigen,
            bool esAutomatico,
            bool esReversion,
            int? idAsientoRevertido,
            IEnumerable<CrearAsientoContableDetalleRequest> detalles,
            string usuarioAlta)
        {
            var asiento = new AsientoContable
            {
                Fecha = EnsureUtc(fecha),
                Descripcion = NormalizeRequired(descripcion, "La descripcion del asiento es obligatoria."),
                ModuloOrigen = string.IsNullOrWhiteSpace(moduloOrigen) ? null : moduloOrigen.Trim(),
                IdOrigen = string.IsNullOrWhiteSpace(idOrigen) ? null : idOrigen.Trim(),
                EsAutomatico = esAutomatico,
                EsReversion = esReversion,
                IdAsientoRevertido = idAsientoRevertido,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = usuarioAlta
            };

            foreach (var detalle in detalles)
            {
                asiento.Detalles.Add(new AsientoContableDetalle
                {
                    CuentaContableId = detalle.CuentaContableId,
                    Descripcion = NormalizeRequired(detalle.Descripcion, "La descripcion del renglon es obligatoria."),
                    Debe = NormalizeMoney(detalle.Debe),
                    Haber = NormalizeMoney(detalle.Haber)
                });
            }

            return asiento;
        }

        private async Task ValidateDetallesAsync(IReadOnlyCollection<CrearAsientoContableDetalleRequest> detalles, bool requireActiveAccounts)
        {
            if (detalles.Count < 2)
            {
                throw new InvalidOperationException("Un asiento debe tener al menos dos renglones.");
            }

            var cuentaIds = detalles.Select(d => d.CuentaContableId).Distinct().ToList();
            if (cuentaIds.Any(id => id <= 0))
            {
                throw new InvalidOperationException("Cada renglon debe tener una cuenta contable valida.");
            }

            var cuentas = await _db.CuentasContables
                .Where(c => cuentaIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            foreach (var detalle in detalles)
            {
                if (!cuentas.TryGetValue(detalle.CuentaContableId, out var cuenta))
                {
                    throw new InvalidOperationException("La cuenta contable indicada no existe.");
                }

                if (requireActiveAccounts && !cuenta.Activa)
                {
                    throw new InvalidOperationException("Solo se pueden utilizar cuentas contables activas.");
                }

                ValidateDetalleImportes(detalle);
            }
        }

        private static void ValidateDetalleImportes(CrearAsientoContableDetalleRequest detalle)
        {
            if (detalle.Debe < 0 || detalle.Haber < 0)
            {
                throw new InvalidOperationException("Los importes no pueden ser negativos.");
            }

            if (detalle.Debe > 0 && detalle.Haber > 0)
            {
                throw new InvalidOperationException("Un renglon no puede tener Debe y Haber simultaneamente.");
            }

            if (detalle.Debe <= 0 && detalle.Haber <= 0)
            {
                throw new InvalidOperationException("Cada renglon debe tener un importe mayor a cero en Debe o Haber.");
            }
        }

        private static void ValidateBalance(IEnumerable<CrearAsientoContableDetalleRequest> detalles)
        {
            var totalDebe = detalles.Sum(d => NormalizeMoney(d.Debe));
            var totalHaber = detalles.Sum(d => NormalizeMoney(d.Haber));

            if (totalDebe != totalHaber)
            {
                throw new InvalidOperationException("El asiento esta desbalanceado. El total del Debe debe ser igual al total del Haber.");
            }
        }

        private IQueryable<AsientoContable> GetAsientoDetalleQuery()
        {
            return _db.AsientosContables
                .Include(a => a.AsientoRevertido)
                .Include(a => a.Detalles)
                    .ThenInclude(d => d.CuentaContable);
        }

        private async Task<int?> GetReversionIdAsync(int asientoId)
        {
            return await _db.AsientosContables
                .AsNoTracking()
                .Where(a => a.IdAsientoRevertido == asientoId)
                .OrderBy(a => a.Id)
                .Select(a => (int?)a.Id)
                .FirstOrDefaultAsync();
        }

        private static AsientoContableListResponse MapList(AsientoContable asiento, int? reversionId)
        {
            var totalDebe = asiento.Detalles.Sum(d => d.Debe);
            var totalHaber = asiento.Detalles.Sum(d => d.Haber);

            return new AsientoContableListResponse
            {
                Id = asiento.Id,
                Fecha = asiento.Fecha,
                Descripcion = asiento.Descripcion,
                Tipo = GetTipo(asiento),
                ModuloOrigen = asiento.ModuloOrigen,
                IdOrigen = asiento.IdOrigen,
                TotalDebe = totalDebe,
                TotalHaber = totalHaber,
                Estado = reversionId.HasValue ? "Reversado" : "Normal",
                AsientoReversionId = reversionId,
                UsuarioAlta = asiento.UsuarioAlta,
                FechaAlta = asiento.FechaAlta
            };
        }

        private static AsientoContableResponse MapDetalle(AsientoContable asiento, int? reversionId)
        {
            var response = new AsientoContableResponse
            {
                EsAutomatico = asiento.EsAutomatico,
                EsReversion = asiento.EsReversion,
                IdAsientoRevertido = asiento.IdAsientoRevertido,
                Detalles = asiento.Detalles
                    .OrderBy(d => d.Id)
                    .Select(d => new AsientoContableDetalleResponse
                    {
                        Id = d.Id,
                        CuentaContableId = d.CuentaContableId,
                        CuentaCodigo = d.CuentaContable.Codigo,
                        CuentaNombre = d.CuentaContable.Nombre,
                        Descripcion = d.Descripcion,
                        Debe = d.Debe,
                        Haber = d.Haber
                    })
                    .ToList()
            };

            var list = MapList(asiento, reversionId);
            response.Id = list.Id;
            response.Fecha = list.Fecha;
            response.Descripcion = list.Descripcion;
            response.Tipo = list.Tipo;
            response.ModuloOrigen = list.ModuloOrigen;
            response.IdOrigen = list.IdOrigen;
            response.TotalDebe = list.TotalDebe;
            response.TotalHaber = list.TotalHaber;
            response.Estado = list.Estado;
            response.AsientoReversionId = list.AsientoReversionId;
            response.UsuarioAlta = list.UsuarioAlta;
            response.FechaAlta = list.FechaAlta;
            return response;
        }

        private static string GetTipo(AsientoContable asiento)
        {
            if (asiento.EsReversion) return "Reversion";
            return asiento.EsAutomatico ? "Automatico" : "Manual";
        }

        private static string NormalizeRequired(string? value, string errorMessage)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidOperationException(errorMessage);
            }
            return normalized;
        }

        private static decimal NormalizeMoney(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
