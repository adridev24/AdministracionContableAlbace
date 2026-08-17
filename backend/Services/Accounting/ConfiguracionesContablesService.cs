using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Accounting;
using BudgetControl.Api.Models.Accounting;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Accounting
{
    public class ConfiguracionesContablesService : IConfiguracionesContablesService
    {
        private static readonly IReadOnlyList<TipoOperacionContableResponse> TiposOperacion = new List<TipoOperacionContableResponse>
        {
            new() { Codigo = "FACTURA_VENTA", Descripcion = "Factura de venta", ConceptosSugeridos = new() { "CLIENTES", "VENTA_NETA", "IVA_DEBITO", "PERCEPCION_IIBB" } },
            new() { Codigo = "COBRO_CLIENTE", Descripcion = "Cobro de cliente", ConceptosSugeridos = new() { "CAJA", "BANCO", "CLIENTES" } },
            new() { Codigo = "RETENCION_CLIENTE", Descripcion = "Retencion de cliente", ConceptosSugeridos = new() { "RETENCIONES", "CLIENTES" } },
            new() { Codigo = "ANULACION_FACTURA_VENTA", Descripcion = "Anulacion de factura de venta", ConceptosSugeridos = new() { "CLIENTES", "VENTA_NETA", "IVA_DEBITO" } },
            new() { Codigo = "ANULACION_COBRO_CLIENTE", Descripcion = "Anulacion de cobro de cliente", ConceptosSugeridos = new() { "CAJA", "BANCO", "CLIENTES" } }
        };

        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;

        public ConfiguracionesContablesService(AppDbContext db, IUserContext userContext)
        {
            _db = db;
            _userContext = userContext;
        }

        public Task<IEnumerable<TipoOperacionContableResponse>> GetTiposOperacionAsync()
        {
            return Task.FromResult(TiposOperacion.AsEnumerable());
        }

        public async Task<IEnumerable<ConfiguracionContableListResponse>> GetConfiguracionesAsync(ConfiguracionContableFilter filter)
        {
            var query = _db.ConfiguracionesContables
                .AsNoTracking()
                .Include(c => c.Detalles)
                .AsQueryable();

            var codigo = NormalizeCodigoOrEmpty(filter.CodigoOperacion);
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                query = query.Where(c => c.CodigoOperacion.Contains(codigo));
            }

            if (filter.Activa.HasValue)
            {
                query = query.Where(c => c.Activa == filter.Activa.Value);
            }

            var configuraciones = await query
                .OrderBy(c => c.CodigoOperacion)
                .ToListAsync();

            return configuraciones.Select(MapList).ToList();
        }

        public async Task<ConfiguracionContableResponse?> GetConfiguracionAsync(int id)
        {
            var configuracion = await GetDetalleQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            return configuracion == null ? null : MapDetalle(configuracion);
        }

        public async Task<ConfiguracionContableResponse?> GetConfiguracionPorOperacionAsync(string codigoOperacion)
        {
            var codigo = NormalizeCodigo(codigoOperacion);
            var configuracion = await GetDetalleQuery()
                .AsNoTracking()
                .Where(c => c.Activa && c.CodigoOperacion == codigo)
                .FirstOrDefaultAsync();

            return configuracion == null ? null : MapDetalle(configuracion);
        }

        public async Task<ConfiguracionContableResponse> CreateConfiguracionAsync(UpsertConfiguracionContableRequest request)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var codigo = NormalizeCodigo(request.CodigoOperacion);
            var descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            await EnsureCodigoDisponibleAsync(codigo);
            var detalles = await NormalizeAndValidateDetallesAsync(request.Detalles);

            var configuracion = new ConfiguracionContable
            {
                CodigoOperacion = codigo,
                Descripcion = descripcion,
                Activa = true,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            foreach (var detalle in detalles)
            {
                configuracion.Detalles.Add(BuildDetalle(detalle));
            }

            _db.ConfiguracionesContables.Add(configuracion);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (await GetConfiguracionAsync(configuracion.Id))!;
        }

        public async Task<ConfiguracionContableResponse> UpdateConfiguracionAsync(int id, UpsertConfiguracionContableRequest request)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var configuracion = await _db.ConfiguracionesContables
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (configuracion == null)
            {
                throw new KeyNotFoundException("Configuracion contable no encontrada.");
            }

            var codigo = NormalizeCodigo(request.CodigoOperacion);
            var descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            if (!string.Equals(configuracion.CodigoOperacion, codigo, StringComparison.OrdinalIgnoreCase))
            {
                await EnsureCodigoDisponibleAsync(codigo, id);
                configuracion.CodigoOperacion = codigo;
            }

            var detalles = await NormalizeAndValidateDetallesAsync(request.Detalles);
            configuracion.Descripcion = descripcion;
            configuracion.Activa = request.Activa;

            foreach (var detalle in configuracion.Detalles.Where(d => d.Activo))
            {
                detalle.Activo = false;
            }

            foreach (var detalle in detalles)
            {
                configuracion.Detalles.Add(BuildDetalle(detalle));
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (await GetConfiguracionAsync(configuracion.Id))!;
        }

        public async Task<bool> DarDeBajaAsync(int id)
        {
            var configuracion = await _db.ConfiguracionesContables.FirstOrDefaultAsync(c => c.Id == id);
            if (configuracion == null)
            {
                return false;
            }

            configuracion.Activa = false;
            await _db.SaveChangesAsync();
            return true;
        }

        private async Task EnsureCodigoDisponibleAsync(string codigo, int? excludeId = null)
        {
            var exists = await _db.ConfiguracionesContables
                .AnyAsync(c => c.CodigoOperacion == codigo && (!excludeId.HasValue || c.Id != excludeId.Value));

            if (exists)
            {
                throw new InvalidOperationException("Ya existe una configuracion contable con ese codigo de operacion.");
            }
        }

        private async Task<List<UpsertConfiguracionContableDetalleRequest>> NormalizeAndValidateDetallesAsync(IReadOnlyCollection<UpsertConfiguracionContableDetalleRequest> detalles)
        {
            if (detalles.Count < 2)
            {
                throw new InvalidOperationException("La configuracion debe tener al menos dos detalles.");
            }

            var normalized = detalles.Select((detalle, index) => new UpsertConfiguracionContableDetalleRequest
            {
                TipoMovimiento = NormalizeTipoMovimiento(detalle.TipoMovimiento),
                Concepto = NormalizeConcepto(detalle.Concepto),
                CuentaContableId = detalle.CuentaContableId,
                Orden = detalle.Orden > 0 ? detalle.Orden : index + 1,
                EsObligatorio = detalle.EsObligatorio
            }).ToList();

            if (!normalized.Any(d => d.TipoMovimiento == "Debe"))
            {
                throw new InvalidOperationException("La configuracion debe tener al menos un movimiento Debe.");
            }

            if (!normalized.Any(d => d.TipoMovimiento == "Haber"))
            {
                throw new InvalidOperationException("La configuracion debe tener al menos un movimiento Haber.");
            }

            var conceptoDuplicado = normalized
                .GroupBy(d => d.Concepto, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);

            if (conceptoDuplicado != null)
            {
                throw new InvalidOperationException("No se puede repetir el mismo concepto dentro de una operacion.");
            }

            var cuentaIds = normalized.Select(d => d.CuentaContableId).Distinct().ToList();
            if (cuentaIds.Any(id => id <= 0))
            {
                throw new InvalidOperationException("Cada detalle debe indicar una cuenta contable.");
            }

            var cuentas = await _db.CuentasContables
                .Where(c => cuentaIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            foreach (var detalle in normalized)
            {
                if (!cuentas.TryGetValue(detalle.CuentaContableId, out var cuenta))
                {
                    throw new InvalidOperationException("La cuenta contable indicada no existe.");
                }

                if (!cuenta.Activa)
                {
                    throw new InvalidOperationException("Solo se pueden utilizar cuentas contables activas.");
                }
            }

            return normalized.OrderBy(d => d.Orden).ToList();
        }

        private static ConfiguracionContableDetalle BuildDetalle(UpsertConfiguracionContableDetalleRequest detalle)
        {
            return new ConfiguracionContableDetalle
            {
                TipoMovimiento = detalle.TipoMovimiento,
                Concepto = detalle.Concepto,
                CuentaContableId = detalle.CuentaContableId,
                Orden = detalle.Orden,
                EsObligatorio = detalle.EsObligatorio,
                Activo = true
            };
        }

        private IQueryable<ConfiguracionContable> GetDetalleQuery()
        {
            return _db.ConfiguracionesContables
                .Include(c => c.Detalles.Where(d => d.Activo))
                    .ThenInclude(d => d.CuentaContable);
        }

        private static ConfiguracionContableListResponse MapList(ConfiguracionContable configuracion)
        {
            return new ConfiguracionContableListResponse
            {
                Id = configuracion.Id,
                CodigoOperacion = configuracion.CodigoOperacion,
                Descripcion = configuracion.Descripcion,
                Activa = configuracion.Activa,
                CantidadCuentasConfiguradas = configuracion.Detalles.Count(d => d.Activo),
                FechaAlta = configuracion.FechaAlta,
                UsuarioAlta = configuracion.UsuarioAlta
            };
        }

        private static ConfiguracionContableResponse MapDetalle(ConfiguracionContable configuracion)
        {
            var response = new ConfiguracionContableResponse
            {
                Id = configuracion.Id,
                CodigoOperacion = configuracion.CodigoOperacion,
                Descripcion = configuracion.Descripcion,
                Activa = configuracion.Activa,
                CantidadCuentasConfiguradas = configuracion.Detalles.Count(d => d.Activo),
                FechaAlta = configuracion.FechaAlta,
                UsuarioAlta = configuracion.UsuarioAlta,
                Detalles = configuracion.Detalles
                    .Where(d => d.Activo)
                    .OrderBy(d => d.Orden)
                    .Select(d => new ConfiguracionContableDetalleResponse
                    {
                        Id = d.Id,
                        TipoMovimiento = d.TipoMovimiento,
                        Concepto = d.Concepto,
                        CuentaContableId = d.CuentaContableId,
                        CuentaCodigo = d.CuentaContable.Codigo,
                        CuentaNombre = d.CuentaContable.Nombre,
                        Orden = d.Orden,
                        EsObligatorio = d.EsObligatorio,
                        Activo = d.Activo
                    })
                    .ToList()
            };

            return response;
        }

        private static string NormalizeCodigo(string? codigo)
        {
            var normalized = NormalizeCodigoOrEmpty(codigo);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidOperationException("El codigo de operacion es obligatorio.");
            }
            return normalized;
        }

        private static string NormalizeCodigoOrEmpty(string? codigo)
        {
            return codigo?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        private static string NormalizeRequired(string? value, string message)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidOperationException(message);
            }
            return normalized;
        }

        private static string NormalizeConcepto(string? concepto)
        {
            var normalized = concepto?.Trim().ToUpperInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidOperationException("El concepto es obligatorio.");
            }
            return normalized;
        }

        private static string NormalizeTipoMovimiento(string? tipoMovimiento)
        {
            var normalized = tipoMovimiento?.Trim().ToLowerInvariant() ?? string.Empty;
            return normalized switch
            {
                "debe" => "Debe",
                "haber" => "Haber",
                _ => throw new InvalidOperationException("El tipo de movimiento solo puede ser Debe o Haber.")
            };
        }
    }
}
