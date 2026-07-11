using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Sales;
using BudgetControl.Api.Models;
using BudgetControl.Api.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Sales
{
    public class VentasService : IVentasService
    {
        private const string MonedaBase = "ARS";
        private const int MaxPageSize = 100;

        private readonly AppDbContext _db;
        private readonly IExternalDataService _externalDataService;
        private readonly IUserContext _userContext;

        public VentasService(AppDbContext db, IExternalDataService externalDataService, IUserContext userContext)
        {
            _db = db;
            _externalDataService = externalDataService;
            _userContext = userContext;
        }

        public async Task<IEnumerable<TipoComprobanteVentaResponse>> GetTiposComprobanteAsync(bool soloActivos = false)
        {
            var query = _db.TiposComprobanteVenta.AsNoTracking();
            if (soloActivos)
            {
                query = query.Where(t => t.Activo);
            }

            var tipos = await query
                .OrderBy(t => t.Orden)
                .ThenBy(t => t.Descripcion)
                .ToListAsync();

            return tipos.Select(MapTipo);
        }

        public async Task<VentaListResponse> GetVentasAsync(VentaListFilterRequest filters)
        {
            var page = Math.Max(filters.Page, 1);
            var pageSize = Math.Clamp(filters.PageSize, 1, MaxPageSize);

            var query = GetVentaQuery();

            if (filters.FechaDesde.HasValue)
            {
                var desde = EnsureUtc(filters.FechaDesde.Value.Date);
                query = query.Where(v => v.FechaComprobante >= desde);
            }

            if (filters.FechaHasta.HasValue)
            {
                var hasta = EnsureUtc(filters.FechaHasta.Value.Date.AddDays(1).AddTicks(-1));
                query = query.Where(v => v.FechaComprobante <= hasta);
            }

            if (!string.IsNullOrWhiteSpace(filters.ClienteExternoId))
            {
                var clienteId = filters.ClienteExternoId.Trim();
                query = query.Where(v => v.ClienteExternoId == clienteId);
            }

            if (!string.IsNullOrWhiteSpace(filters.ObraExternaId))
            {
                var obraId = filters.ObraExternaId.Trim();
                query = query.Where(v => v.ObraExternaId == obraId);
            }

            if (filters.TipoComprobanteVentaId.HasValue)
            {
                query = query.Where(v => v.TipoComprobanteVentaId == filters.TipoComprobanteVentaId.Value);
            }

            if (filters.PuntoVenta.HasValue)
            {
                query = query.Where(v => v.PuntoVenta == filters.PuntoVenta.Value);
            }

            if (filters.NumeroComprobante.HasValue)
            {
                query = query.Where(v => v.NumeroComprobante == filters.NumeroComprobante.Value);
            }

            if (filters.Estado.HasValue)
            {
                query = query.Where(v => v.Estado == filters.Estado.Value);
            }

            var total = await query.CountAsync();
            var ventas = await query
                .OrderByDescending(v => v.FechaComprobante)
                .ThenByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var lookups = await LoadLookupsAsync(ventas);
            return new VentaListResponse
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = ventas.Select(v => MapVenta(v, lookups.Clientes, lookups.Obras)).ToList()
            };
        }

        public async Task<VentaResponse?> GetVentaAsync(int id)
        {
            var venta = await _db.Ventas
                .Include(v => v.TipoComprobante)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (venta == null)
            {
                return null;
            }

            var lookups = await LoadLookupsAsync(new[] { venta });
            return MapVenta(venta, lookups.Clientes, lookups.Obras);
        }

        public async Task<VentaResponse> CreateVentaAsync(VentaHeaderRequest request)
        {
            var normalized = await ValidateRequestAsync(request, null);
            var usuario = _userContext.UserName;

            var venta = new Venta
            {
                TipoComprobanteVentaId = normalized.Tipo.Id,
                ClienteExternoId = normalized.Cliente.IdCliente.ToString(),
                ObraExternaId = normalized.Obra.IdObra.ToString(),
                FechaComprobante = EnsureUtc(request.FechaComprobante),
                PuntoVenta = request.PuntoVenta,
                NumeroComprobante = request.NumeroComprobante,
                MonedaCodigo = NormalizeCurrency(request.MonedaCodigo),
                Cotizacion = request.Cotizacion,
                Estado = VentaEstado.Borrador,
                Observaciones = NormalizeOptional(request.Observaciones),
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = usuario
            };

            _db.Ventas.Add(venta);
            await _db.SaveChangesAsync();

            venta.TipoComprobante = normalized.Tipo;
            return MapVenta(venta, new Dictionary<string, Client> { [venta.ClienteExternoId] = normalized.Cliente }, new Dictionary<string, Obra> { [venta.ObraExternaId] = normalized.Obra });
        }

        public async Task<VentaResponse> UpdateVentaAsync(int id, VentaHeaderRequest request)
        {
            var venta = await GetVentaQuery().FirstOrDefaultAsync(v => v.Id == id);
            if (venta == null)
            {
                throw new InvalidOperationException("Venta no encontrada.");
            }

            if (venta.Estado != VentaEstado.Borrador)
            {
                throw new InvalidOperationException("Solo una venta en estado Borrador puede modificarse.");
            }

            var normalized = await ValidateRequestAsync(request, id);

            venta.TipoComprobanteVentaId = normalized.Tipo.Id;
            venta.ClienteExternoId = normalized.Cliente.IdCliente.ToString();
            venta.ObraExternaId = normalized.Obra.IdObra.ToString();
            venta.FechaComprobante = EnsureUtc(request.FechaComprobante);
            venta.PuntoVenta = request.PuntoVenta;
            venta.NumeroComprobante = request.NumeroComprobante;
            venta.MonedaCodigo = NormalizeCurrency(request.MonedaCodigo);
            venta.Cotizacion = request.Cotizacion;
            venta.Observaciones = NormalizeOptional(request.Observaciones);
            venta.FechaModificacion = DateTime.UtcNow;
            venta.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();

            venta.TipoComprobante = normalized.Tipo;
            return MapVenta(venta, new Dictionary<string, Client> { [venta.ClienteExternoId] = normalized.Cliente }, new Dictionary<string, Obra> { [venta.ObraExternaId] = normalized.Obra });
        }

        private IQueryable<Venta> GetVentaQuery()
        {
            return _db.Ventas
                .AsNoTracking()
                .Include(v => v.TipoComprobante);
        }

        private async Task<ValidatedVentaRequest> ValidateRequestAsync(VentaHeaderRequest request, int? ventaId)
        {
            var tipo = await _db.TiposComprobanteVenta.FirstOrDefaultAsync(t => t.Id == request.TipoComprobanteVentaId);
            if (tipo == null)
            {
                throw new InvalidOperationException("Tipo de comprobante no encontrado.");
            }

            if (!tipo.Activo)
            {
                throw new InvalidOperationException("El tipo de comprobante se encuentra inactivo.");
            }

            if (!int.TryParse(request.ClienteExternoId, out var clienteId))
            {
                throw new InvalidOperationException("Cliente invalido.");
            }

            if (!int.TryParse(request.ObraExternaId, out var obraId))
            {
                throw new InvalidOperationException("Obra invalida.");
            }

            var cliente = await _externalDataService.GetClientByIdAsync(clienteId);
            if (cliente == null)
            {
                throw new InvalidOperationException("Cliente no encontrado.");
            }

            var obra = await _externalDataService.GetObraByIdAsync(obraId);
            if (obra == null)
            {
                throw new InvalidOperationException("Obra no encontrada.");
            }

            if (obra.ClienteId != cliente.IdCliente)
            {
                throw new InvalidOperationException("La obra seleccionada no pertenece al cliente indicado.");
            }

            if (request.FechaComprobante == default)
            {
                throw new InvalidOperationException("La fecha del comprobante es obligatoria.");
            }

            if (request.PuntoVenta <= 0)
            {
                throw new InvalidOperationException("El punto de venta debe ser mayor que cero.");
            }

            if (request.NumeroComprobante <= 0)
            {
                throw new InvalidOperationException("El numero de comprobante debe ser mayor que cero.");
            }

            var moneda = NormalizeCurrency(request.MonedaCodigo);
            if (string.IsNullOrWhiteSpace(moneda))
            {
                throw new InvalidOperationException("La moneda es obligatoria.");
            }

            if (moneda == MonedaBase && request.Cotizacion != 1)
            {
                throw new InvalidOperationException("La cotizacion de la moneda base debe ser 1.");
            }

            if (moneda != MonedaBase && request.Cotizacion <= 0)
            {
                throw new InvalidOperationException("La cotizacion debe ser mayor que cero.");
            }

            var duplicateQuery = _db.Ventas.Where(v =>
                v.TipoComprobanteVentaId == request.TipoComprobanteVentaId &&
                v.PuntoVenta == request.PuntoVenta &&
                v.NumeroComprobante == request.NumeroComprobante);

            if (ventaId.HasValue)
            {
                duplicateQuery = duplicateQuery.Where(v => v.Id != ventaId.Value);
            }

            if (await duplicateQuery.AnyAsync())
            {
                throw new InvalidOperationException("Ya existe una venta con el mismo tipo, punto de venta y numero de comprobante.");
            }

            return new ValidatedVentaRequest(tipo, cliente, obra);
        }

        private async Task<(Dictionary<string, Client> Clientes, Dictionary<string, Obra> Obras)> LoadLookupsAsync(IEnumerable<Venta> ventas)
        {
            var clientes = new Dictionary<string, Client>();
            var obras = new Dictionary<string, Obra>();

            foreach (var clienteId in ventas.Select(v => v.ClienteExternoId).Distinct())
            {
                if (int.TryParse(clienteId, out var parsedId))
                {
                    var cliente = await _externalDataService.GetClientByIdAsync(parsedId);
                    if (cliente != null)
                    {
                        clientes[clienteId] = cliente;
                    }
                }
            }

            foreach (var obraId in ventas.Select(v => v.ObraExternaId).Distinct())
            {
                if (int.TryParse(obraId, out var parsedId))
                {
                    var obra = await _externalDataService.GetObraByIdAsync(parsedId);
                    if (obra != null)
                    {
                        obras[obraId] = obra;
                    }
                }
            }

            return (clientes, obras);
        }

        private static TipoComprobanteVentaResponse MapTipo(TipoComprobanteVenta tipo)
        {
            return new TipoComprobanteVentaResponse
            {
                Id = tipo.Id,
                Codigo = tipo.Codigo,
                Descripcion = tipo.Descripcion,
                Letra = tipo.Letra,
                Signo = tipo.Signo,
                Activo = tipo.Activo,
                Orden = tipo.Orden
            };
        }

        private static VentaResponse MapVenta(Venta venta, IReadOnlyDictionary<string, Client> clientes, IReadOnlyDictionary<string, Obra> obras)
        {
            clientes.TryGetValue(venta.ClienteExternoId, out var cliente);
            obras.TryGetValue(venta.ObraExternaId, out var obra);

            return new VentaResponse
            {
                Id = venta.Id,
                TipoComprobanteVentaId = venta.TipoComprobanteVentaId,
                TipoComprobanteCodigo = venta.TipoComprobante.Codigo,
                TipoComprobanteDescripcion = venta.TipoComprobante.Descripcion,
                TipoComprobanteLetra = venta.TipoComprobante.Letra,
                ClienteExternoId = venta.ClienteExternoId,
                ClienteNombre = cliente?.NombreCliente,
                ObraExternaId = venta.ObraExternaId,
                ObraNombre = obra?.NombreObra,
                FechaComprobante = venta.FechaComprobante,
                PuntoVenta = venta.PuntoVenta,
                NumeroComprobante = venta.NumeroComprobante,
                MonedaCodigo = venta.MonedaCodigo,
                Cotizacion = venta.Cotizacion,
                Estado = venta.Estado,
                Observaciones = venta.Observaciones,
                FechaAlta = venta.FechaAlta,
                UsuarioAlta = venta.UsuarioAlta,
                FechaModificacion = venta.FechaModificacion,
                UsuarioModificacion = venta.UsuarioModificacion
            };
        }

        private static string NormalizeCurrency(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private sealed record ValidatedVentaRequest(TipoComprobanteVenta Tipo, Client Cliente, Obra Obra);
    }
}
