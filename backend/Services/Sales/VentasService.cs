using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Sales;
using BudgetControl.Api.Models;
using BudgetControl.Api.Models.Sales;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
            if (soloActivos) query = query.Where(t => t.Activo);

            var tipos = await query.OrderBy(t => t.Orden).ThenBy(t => t.Descripcion).ToListAsync();
            return tipos.Select(MapTipo);
        }

        public async Task<TipoComprobanteVentaResponse?> GetTipoComprobanteAsync(int id)
        {
            var tipo = await _db.TiposComprobanteVenta.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            return tipo == null ? null : MapTipo(tipo);
        }

        public async Task<TipoComprobanteVentaResponse> CreateTipoComprobanteAsync(TipoComprobanteVentaRequest request)
        {
            var codigo = NormalizeCode(request.Codigo);
            await ValidateTipoComprobanteRequestAsync(request, codigo, null);

            var tipo = new TipoComprobanteVenta
            {
                Codigo = codigo,
                Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria."),
                Letra = NormalizeOptional(request.Letra),
                TipoFiscal = NormalizeTipoFiscal(request),
                EsCreditoElectronica = request.EsCreditoElectronica,
                EsExportacion = request.EsExportacion,
                RequiereNomenclador = request.RequiereNomenclador,
                PermiteIva = request.PermiteIva,
                Signo = NormalizeSigno(request.Signo),
                Activo = request.Activo,
                Orden = request.Orden,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            _db.TiposComprobanteVenta.Add(tipo);
            await _db.SaveChangesAsync();
            return MapTipo(tipo);
        }

        public async Task<TipoComprobanteVentaResponse> UpdateTipoComprobanteAsync(int id, TipoComprobanteVentaRequest request)
        {
            var tipo = await _db.TiposComprobanteVenta.FirstOrDefaultAsync(t => t.Id == id);
            if (tipo == null) throw new InvalidOperationException("Configuracion de comprobante no encontrada.");

            var codigo = NormalizeCode(request.Codigo);
            await ValidateTipoComprobanteRequestAsync(request, codigo, id);

            tipo.Codigo = codigo;
            tipo.Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            tipo.Letra = NormalizeOptional(request.Letra);
            tipo.TipoFiscal = NormalizeTipoFiscal(request);
            tipo.EsCreditoElectronica = request.EsCreditoElectronica;
            tipo.EsExportacion = request.EsExportacion;
            tipo.RequiereNomenclador = request.RequiereNomenclador;
            tipo.PermiteIva = request.PermiteIva;
            tipo.Signo = NormalizeSigno(request.Signo);
            tipo.Activo = request.Activo;
            tipo.Orden = request.Orden;
            tipo.FechaModificacion = DateTime.UtcNow;
            tipo.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            return MapTipo(tipo);
        }

        public async Task<IEnumerable<PuntoVentaResponse>> GetPuntosVentaAsync(bool soloActivos = false)
        {
            var query = _db.PuntosVenta
                .AsNoTracking()
                .Include(p => p.Comprobantes)
                    .ThenInclude(r => r.TipoComprobante)
                .AsQueryable();
            if (soloActivos) query = query.Where(p => p.Activo);

            var puntos = await query.OrderBy(p => p.Numero).ToListAsync();
            return puntos.Select(MapPuntoVenta);
        }

        public async Task<PuntoVentaResponse?> GetPuntoVentaAsync(int id)
        {
            var punto = await _db.PuntosVenta
                .AsNoTracking()
                .Include(p => p.Comprobantes)
                    .ThenInclude(r => r.TipoComprobante)
                .FirstOrDefaultAsync(p => p.Id == id);
            return punto == null ? null : MapPuntoVenta(punto);
        }

        public async Task<PuntoVentaResponse> CreatePuntoVentaAsync(PuntoVentaRequest request)
        {
            await ValidatePuntoVentaRequestAsync(request, null);
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var punto = new PuntoVenta
            {
                Numero = request.Numero,
                Descripcion = NormalizeRequired(request.Descripcion, "La descripcion del punto de venta es obligatoria."),
                Activo = request.Activo,
                Observaciones = NormalizeOptional(request.Observaciones),
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            _db.PuntosVenta.Add(punto);
            await _db.SaveChangesAsync();

            if (request.ComprobantesPermitidosIds != null)
            {
                await SyncComprobantesPermitidosAsync(punto, request.ComprobantesPermitidosIds);
            }

            await transaction.CommitAsync();
            return MapPuntoVenta(punto);
        }

        public async Task<PuntoVentaResponse> UpdatePuntoVentaAsync(int id, PuntoVentaRequest request)
        {
            var punto = await _db.PuntosVenta
                .Include(p => p.Comprobantes)
                    .ThenInclude(r => r.TipoComprobante)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (punto == null) throw new InvalidOperationException("Punto de venta no encontrado.");

            await ValidatePuntoVentaRequestAsync(request, id);
            await using var transaction = await _db.Database.BeginTransactionAsync();
            punto.Numero = request.Numero;
            punto.Descripcion = NormalizeRequired(request.Descripcion, "La descripcion del punto de venta es obligatoria.");
            punto.Activo = request.Activo;
            punto.Observaciones = NormalizeOptional(request.Observaciones);
            punto.FechaModificacion = DateTime.UtcNow;
            punto.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();

            if (request.ComprobantesPermitidosIds != null)
            {
                await SyncComprobantesPermitidosAsync(punto, request.ComprobantesPermitidosIds);
            }

            await transaction.CommitAsync();
            return MapPuntoVenta(punto);
        }

        public async Task<IEnumerable<PuntoVentaComprobanteResponse>> GetComprobantesPorPuntoVentaAsync(int puntoVentaId, bool soloActivos = false)
        {
            var query = GetPuntoVentaComprobanteQuery().Where(r => r.PuntoVentaId == puntoVentaId);
            if (soloActivos) query = query.Where(r => r.Activo);

            var relaciones = await query
                .OrderBy(r => r.TipoComprobante.Orden)
                .ThenBy(r => r.TipoComprobante.Descripcion)
                .ToListAsync();

            return relaciones.Select(MapPuntoVentaComprobante);
        }

        public async Task<PuntoVentaComprobanteResponse> CreatePuntoVentaComprobanteAsync(int puntoVentaId, PuntoVentaComprobanteRequest request)
        {
            var punto = await _db.PuntosVenta.FirstOrDefaultAsync(p => p.Id == puntoVentaId);
            if (punto == null) throw new InvalidOperationException("Punto de venta no encontrado.");

            var tipo = await _db.TiposComprobanteVenta.FirstOrDefaultAsync(t => t.Id == request.TipoComprobanteVentaId);
            if (tipo == null) throw new InvalidOperationException("Configuracion de comprobante no encontrada.");

            if (await _db.PuntosVentaComprobantes.AnyAsync(r => r.PuntoVentaId == puntoVentaId && r.TipoComprobanteVentaId == request.TipoComprobanteVentaId))
            {
                throw new InvalidOperationException("El punto de venta ya tiene habilitada esa configuracion de comprobante.");
            }

            var relacion = new PuntoVentaComprobante
            {
                PuntoVentaId = puntoVentaId,
                TipoComprobanteVentaId = request.TipoComprobanteVentaId,
                Activo = request.Activo,
                Descripcion = NormalizeOptional(request.Descripcion),
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            _db.PuntosVentaComprobantes.Add(relacion);
            await _db.SaveChangesAsync();

            relacion.PuntoVenta = punto;
            relacion.TipoComprobante = tipo;
            return MapPuntoVentaComprobante(relacion);
        }

        public async Task<PuntoVentaComprobanteResponse> UpdatePuntoVentaComprobanteAsync(int puntoVentaId, int relacionId, PuntoVentaComprobanteRequest request)
        {
            var relacion = await _db.PuntosVentaComprobantes
                .Include(r => r.PuntoVenta)
                .Include(r => r.TipoComprobante)
                .FirstOrDefaultAsync(r => r.Id == relacionId && r.PuntoVentaId == puntoVentaId);

            if (relacion == null) throw new InvalidOperationException("Relacion punto de venta-comprobante no encontrada.");

            var tipo = await _db.TiposComprobanteVenta.FirstOrDefaultAsync(t => t.Id == request.TipoComprobanteVentaId);
            if (tipo == null) throw new InvalidOperationException("Configuracion de comprobante no encontrada.");

            if (await _db.PuntosVentaComprobantes.AnyAsync(r =>
                r.Id != relacionId &&
                r.PuntoVentaId == puntoVentaId &&
                r.TipoComprobanteVentaId == request.TipoComprobanteVentaId))
            {
                throw new InvalidOperationException("El punto de venta ya tiene habilitada esa configuracion de comprobante.");
            }

            relacion.TipoComprobanteVentaId = request.TipoComprobanteVentaId;
            relacion.TipoComprobante = tipo;
            relacion.Activo = request.Activo;
            relacion.Descripcion = NormalizeOptional(request.Descripcion);
            relacion.FechaModificacion = DateTime.UtcNow;
            relacion.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            return MapPuntoVentaComprobante(relacion);
        }

        public async Task<IEnumerable<AlicuotaIvaVentaResponse>> GetAlicuotasIvaAsync(bool soloActivos = false, string? search = null)
        {
            var query = _db.AlicuotasIvaVenta.AsNoTracking();
            if (soloActivos) query = query.Where(a => a.Activo);
            query = ApplySearch(query, search, a => a.Codigo, a => a.Descripcion);

            var items = await query.OrderBy(a => a.Orden).ThenBy(a => a.Descripcion).ToListAsync();
            return items.Select(MapAlicuotaIva);
        }

        public async Task<AlicuotaIvaVentaResponse?> GetAlicuotaIvaAsync(int id)
        {
            var item = await _db.AlicuotasIvaVenta.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            return item == null ? null : MapAlicuotaIva(item);
        }

        public async Task<AlicuotaIvaVentaResponse> CreateAlicuotaIvaAsync(AlicuotaIvaVentaRequest request)
        {
            var codigo = NormalizeCode(request.Codigo);
            await ValidateAlicuotaIvaRequestAsync(request, codigo, null);

            var item = new AlicuotaIvaVenta
            {
                Codigo = codigo,
                Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria."),
                TipoTratamiento = request.TipoTratamiento,
                Porcentaje = request.Porcentaje,
                Activo = request.Activo,
                Orden = request.Orden,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            _db.AlicuotasIvaVenta.Add(item);
            await _db.SaveChangesAsync();
            return MapAlicuotaIva(item);
        }

        public async Task<AlicuotaIvaVentaResponse> UpdateAlicuotaIvaAsync(int id, AlicuotaIvaVentaRequest request)
        {
            var item = await _db.AlicuotasIvaVenta.FirstOrDefaultAsync(a => a.Id == id);
            if (item == null) throw new InvalidOperationException("Alicuota de IVA no encontrada.");

            var codigo = NormalizeCode(request.Codigo);
            await ValidateAlicuotaIvaRequestAsync(request, codigo, id);

            item.Codigo = codigo;
            item.Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            item.TipoTratamiento = request.TipoTratamiento;
            item.Porcentaje = request.Porcentaje;
            item.Activo = request.Activo;
            item.Orden = request.Orden;
            item.FechaModificacion = DateTime.UtcNow;
            item.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            return MapAlicuotaIva(item);
        }

        public async Task<IEnumerable<NomencladorFceResponse>> GetNomencladoresFceAsync(bool soloActivos = false, string? search = null)
        {
            var query = _db.NomencladoresFce.AsNoTracking();
            if (soloActivos) query = query.Where(n => n.Activo);
            query = ApplySearch(query, search, n => n.Codigo, n => n.Descripcion);

            var items = await query.OrderBy(n => n.Orden).ThenBy(n => n.Descripcion).ToListAsync();
            return items.Select(MapNomencladorFce);
        }

        public async Task<NomencladorFceResponse?> GetNomencladorFceAsync(int id)
        {
            var item = await _db.NomencladoresFce.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
            return item == null ? null : MapNomencladorFce(item);
        }

        public async Task<NomencladorFceResponse> CreateNomencladorFceAsync(NomencladorFceRequest request)
        {
            var codigo = NormalizeCode(request.Codigo);
            await ValidateNomencladorFceRequestAsync(request, codigo, null);

            var item = new NomencladorFce
            {
                Codigo = codigo,
                Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria."),
                Activo = request.Activo,
                Orden = request.Orden,
                Observaciones = NormalizeOptional(request.Observaciones),
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            _db.NomencladoresFce.Add(item);
            await _db.SaveChangesAsync();
            return MapNomencladorFce(item);
        }

        public async Task<NomencladorFceResponse> UpdateNomencladorFceAsync(int id, NomencladorFceRequest request)
        {
            var item = await _db.NomencladoresFce.FirstOrDefaultAsync(n => n.Id == id);
            if (item == null) throw new InvalidOperationException("Nomenclador FCE no encontrado.");

            var codigo = NormalizeCode(request.Codigo);
            await ValidateNomencladorFceRequestAsync(request, codigo, id);

            item.Codigo = codigo;
            item.Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            item.Activo = request.Activo;
            item.Orden = request.Orden;
            item.Observaciones = NormalizeOptional(request.Observaciones);
            item.FechaModificacion = DateTime.UtcNow;
            item.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            return MapNomencladorFce(item);
        }

        public async Task<IEnumerable<PercepcionIibbEntreRiosResponse>> GetPercepcionesIibbAsync(bool soloActivos = false, string? search = null, bool? soloVigentes = null)
        {
            var today = DateTime.UtcNow.Date;
            var query = _db.PercepcionesIibbEntreRios.AsNoTracking();
            if (soloActivos) query = query.Where(p => p.Activo);
            if (soloVigentes == true) query = query.Where(p => p.VigenciaDesde <= today && (!p.VigenciaHasta.HasValue || p.VigenciaHasta.Value >= today));
            query = ApplySearch(query, search, p => p.Codigo, p => p.Descripcion, p => p.NumeroRegimen);

            var items = await query.OrderBy(p => p.Orden).ThenBy(p => p.Descripcion).ToListAsync();
            return items.Select(MapPercepcionIibb);
        }

        public async Task<PercepcionIibbEntreRiosResponse?> GetPercepcionIibbAsync(int id)
        {
            var item = await _db.PercepcionesIibbEntreRios.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            return item == null ? null : MapPercepcionIibb(item);
        }

        public async Task<PercepcionIibbEntreRiosResponse> CreatePercepcionIibbAsync(PercepcionIibbEntreRiosRequest request)
        {
            var codigo = NormalizeCode(request.Codigo);
            await ValidatePercepcionIibbRequestAsync(request, codigo, null);

            var item = new PercepcionIibbEntreRios
            {
                Codigo = codigo,
                Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria."),
                Jurisdiccion = NormalizeJurisdiccionEntreRios(request.Jurisdiccion),
                TipoTributo = NormalizeTipoTributoPercepcion(request.TipoTributo),
                NumeroRegimen = NormalizeRequired(request.NumeroRegimen, "El regimen es obligatorio."),
                Porcentaje = request.Porcentaje,
                TipoBaseCalculo = request.TipoBaseCalculo,
                MontoMinimo = request.MontoMinimo,
                VigenciaDesde = NormalizeDateOnlyUtc(request.VigenciaDesde),
                VigenciaHasta = request.VigenciaHasta.HasValue ? NormalizeDateOnlyUtc(request.VigenciaHasta.Value) : null,
                Activo = request.Activo,
                Orden = request.Orden,
                Observaciones = NormalizeOptional(request.Observaciones),
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            _db.PercepcionesIibbEntreRios.Add(item);
            await _db.SaveChangesAsync();
            return MapPercepcionIibb(item);
        }

        public async Task<PercepcionIibbEntreRiosResponse> UpdatePercepcionIibbAsync(int id, PercepcionIibbEntreRiosRequest request)
        {
            var item = await _db.PercepcionesIibbEntreRios.FirstOrDefaultAsync(p => p.Id == id);
            if (item == null) throw new InvalidOperationException("Regimen de percepcion no encontrado.");

            var codigo = NormalizeCode(request.Codigo);
            await ValidatePercepcionIibbRequestAsync(request, codigo, id);

            item.Codigo = codigo;
            item.Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            item.Jurisdiccion = NormalizeJurisdiccionEntreRios(request.Jurisdiccion);
            item.TipoTributo = NormalizeTipoTributoPercepcion(request.TipoTributo);
            item.NumeroRegimen = NormalizeRequired(request.NumeroRegimen, "El regimen es obligatorio.");
            item.Porcentaje = request.Porcentaje;
            item.TipoBaseCalculo = request.TipoBaseCalculo;
            item.MontoMinimo = request.MontoMinimo;
            item.VigenciaDesde = NormalizeDateOnlyUtc(request.VigenciaDesde);
            item.VigenciaHasta = request.VigenciaHasta.HasValue ? NormalizeDateOnlyUtc(request.VigenciaHasta.Value) : null;
            item.Activo = request.Activo;
            item.Orden = request.Orden;
            item.Observaciones = NormalizeOptional(request.Observaciones);
            item.FechaModificacion = DateTime.UtcNow;
            item.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            return MapPercepcionIibb(item);
        }

        public async Task<VentaListResponse> GetVentasAsync(VentaListFilterRequest filters)
        {
            var page = Math.Max(filters.Page, 1);
            var pageSize = Math.Clamp(filters.PageSize, 1, MaxPageSize);
            var query = GetVentaQuery();

            if (filters.FechaDesde.HasValue) query = query.Where(v => v.FechaComprobante >= EnsureUtc(filters.FechaDesde.Value.Date));
            if (filters.FechaHasta.HasValue) query = query.Where(v => v.FechaComprobante <= EnsureUtc(filters.FechaHasta.Value.Date.AddDays(1).AddTicks(-1)));
            if (!string.IsNullOrWhiteSpace(filters.ClienteExternoId)) query = query.Where(v => v.ClienteExternoId == filters.ClienteExternoId.Trim());
            if (!string.IsNullOrWhiteSpace(filters.ObraExternaId)) query = query.Where(v => v.ObraExternaId == filters.ObraExternaId.Trim());
            if (filters.TipoComprobanteVentaId.HasValue) query = query.Where(v => v.TipoComprobanteVentaId == filters.TipoComprobanteVentaId.Value);
            if (filters.PuntoVentaComprobanteId.HasValue) query = query.Where(v => v.PuntoVentaComprobanteId == filters.PuntoVentaComprobanteId.Value);
            if (filters.PuntoVenta.HasValue) query = query.Where(v => v.PuntoVenta == filters.PuntoVenta.Value);
            if (filters.NumeroComprobante.HasValue) query = query.Where(v => v.NumeroComprobante == filters.NumeroComprobante.Value);
            if (filters.Estado.HasValue) query = query.Where(v => v.Estado == filters.Estado.Value);

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
            var venta = await GetVentaQuery(false).FirstOrDefaultAsync(v => v.Id == id);
            if (venta == null) return null;

            var lookups = await LoadLookupsAsync(new[] { venta });
            return MapVenta(venta, lookups.Clientes, lookups.Obras);
        }

        public async Task<VentaResponse> CreateVentaAsync(VentaHeaderRequest request)
        {
            var normalized = await ValidateRequestAsync(request, null);
            var usuario = _userContext.UserName;

            var venta = new Venta
            {
                TipoComprobanteVentaId = normalized.Relacion.TipoComprobanteVentaId,
                PuntoVentaComprobanteId = normalized.Relacion.Id,
                ClienteExternoId = normalized.Cliente.IdCliente.ToString(),
                ObraExternaId = normalized.Obra.IdObra.ToString(),
                FechaComprobante = EnsureUtc(request.FechaComprobante),
                PuntoVenta = normalized.Relacion.PuntoVenta.Numero,
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

            venta.TipoComprobante = normalized.Relacion.TipoComprobante;
            venta.PuntoVentaComprobante = normalized.Relacion;
            return MapVenta(venta, new Dictionary<string, Client> { [venta.ClienteExternoId] = normalized.Cliente }, new Dictionary<string, Obra> { [venta.ObraExternaId] = normalized.Obra });
        }

        public async Task<VentaResponse> UpdateVentaAsync(int id, VentaHeaderRequest request)
        {
            var venta = await GetVentaQuery(false).FirstOrDefaultAsync(v => v.Id == id);
            if (venta == null) throw new InvalidOperationException("Venta no encontrada.");
            if (venta.Estado != VentaEstado.Borrador) throw new InvalidOperationException("Solo una venta en estado Borrador puede modificarse.");

            var normalized = await ValidateRequestAsync(request, id);

            venta.TipoComprobanteVentaId = normalized.Relacion.TipoComprobanteVentaId;
            venta.PuntoVentaComprobanteId = normalized.Relacion.Id;
            venta.ClienteExternoId = normalized.Cliente.IdCliente.ToString();
            venta.ObraExternaId = normalized.Obra.IdObra.ToString();
            venta.FechaComprobante = EnsureUtc(request.FechaComprobante);
            venta.PuntoVenta = normalized.Relacion.PuntoVenta.Numero;
            venta.NumeroComprobante = request.NumeroComprobante;
            venta.MonedaCodigo = NormalizeCurrency(request.MonedaCodigo);
            venta.Cotizacion = request.Cotizacion;
            venta.Observaciones = NormalizeOptional(request.Observaciones);
            venta.FechaModificacion = DateTime.UtcNow;
            venta.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();

            venta.TipoComprobante = normalized.Relacion.TipoComprobante;
            venta.PuntoVentaComprobante = normalized.Relacion;
            return MapVenta(venta, new Dictionary<string, Client> { [venta.ClienteExternoId] = normalized.Cliente }, new Dictionary<string, Obra> { [venta.ObraExternaId] = normalized.Obra });
        }

        private IQueryable<Venta> GetVentaQuery(bool asNoTracking = true)
        {
            var query = _db.Ventas
                .Include(v => v.TipoComprobante)
                .Include(v => v.PuntoVentaComprobante)
                    .ThenInclude(r => r!.PuntoVenta)
                .Include(v => v.PuntoVentaComprobante)
                    .ThenInclude(r => r!.TipoComprobante);

            return asNoTracking ? query.AsNoTracking() : query;
        }

        private IQueryable<PuntoVentaComprobante> GetPuntoVentaComprobanteQuery()
        {
            return _db.PuntosVentaComprobantes
                .AsNoTracking()
                .Include(r => r.PuntoVenta)
                .Include(r => r.TipoComprobante);
        }

        private async Task<ValidatedVentaRequest> ValidateRequestAsync(VentaHeaderRequest request, int? ventaId)
        {
            var relacion = await ResolveRelacionAsync(request);

            if (!relacion.PuntoVenta.Activo) throw new InvalidOperationException("El punto de venta se encuentra inactivo.");
            if (!relacion.TipoComprobante.Activo) throw new InvalidOperationException("La configuracion de comprobante se encuentra inactiva.");
            if (!relacion.Activo) throw new InvalidOperationException("La combinacion punto de venta-comprobante se encuentra inactiva.");

            if (relacion.TipoComprobante.EsExportacion && !relacion.TipoComprobante.TipoFiscal.Equals("Exportacion", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La configuracion de exportacion no esta correctamente identificada.");
            }

            if (!int.TryParse(request.ClienteExternoId, out var clienteId)) throw new InvalidOperationException("Cliente invalido.");
            if (!int.TryParse(request.ObraExternaId, out var obraId)) throw new InvalidOperationException("Obra invalida.");

            var cliente = await _externalDataService.GetClientByIdAsync(clienteId);
            if (cliente == null) throw new InvalidOperationException("Cliente no encontrado.");

            var obra = await _externalDataService.GetObraByIdAsync(obraId);
            if (obra == null) throw new InvalidOperationException("Obra no encontrada.");
            if (obra.ClienteId != cliente.IdCliente) throw new InvalidOperationException("La obra seleccionada no pertenece al cliente indicado.");
            if (request.FechaComprobante == default) throw new InvalidOperationException("La fecha del comprobante es obligatoria.");
            if (request.NumeroComprobante <= 0) throw new InvalidOperationException("El numero de comprobante debe ser mayor que cero.");

            var moneda = NormalizeCurrency(request.MonedaCodigo);
            if (string.IsNullOrWhiteSpace(moneda)) throw new InvalidOperationException("La moneda es obligatoria.");
            if (moneda == MonedaBase && request.Cotizacion != 1) throw new InvalidOperationException("La cotizacion de la moneda base debe ser 1.");
            if (moneda != MonedaBase && request.Cotizacion <= 0) throw new InvalidOperationException("La cotizacion debe ser mayor que cero.");

            var duplicateQuery = _db.Ventas.Where(v =>
                v.TipoComprobanteVentaId == relacion.TipoComprobanteVentaId &&
                v.PuntoVenta == relacion.PuntoVenta.Numero &&
                v.NumeroComprobante == request.NumeroComprobante);

            if (ventaId.HasValue) duplicateQuery = duplicateQuery.Where(v => v.Id != ventaId.Value);
            if (await duplicateQuery.AnyAsync()) throw new InvalidOperationException("Ya existe una venta con el mismo tipo, punto de venta y numero de comprobante.");

            return new ValidatedVentaRequest(relacion, cliente, obra);
        }

        private async Task<PuntoVentaComprobante> ResolveRelacionAsync(VentaHeaderRequest request)
        {
            var query = _db.PuntosVentaComprobantes
                .Include(r => r.PuntoVenta)
                .Include(r => r.TipoComprobante)
                .AsQueryable();

            if (request.PuntoVentaComprobanteId.HasValue)
            {
                var byId = await query.FirstOrDefaultAsync(r => r.Id == request.PuntoVentaComprobanteId.Value);
                if (byId == null) throw new InvalidOperationException("La combinacion punto de venta-comprobante no existe.");

                if (request.TipoComprobanteVentaId > 0 && request.TipoComprobanteVentaId != byId.TipoComprobanteVentaId)
                {
                    throw new InvalidOperationException("La combinacion no corresponde al tipo de comprobante indicado.");
                }

                if (request.PuntoVenta > 0 && request.PuntoVenta != byId.PuntoVenta.Numero)
                {
                    throw new InvalidOperationException("La combinacion no corresponde al punto de venta indicado.");
                }

                return byId;
            }

            if (request.TipoComprobanteVentaId <= 0) throw new InvalidOperationException("La configuracion de comprobante es obligatoria.");
            if (request.PuntoVenta <= 0) throw new InvalidOperationException("El punto de venta debe ser mayor que cero.");

            var relacion = await query.FirstOrDefaultAsync(r =>
                r.TipoComprobanteVentaId == request.TipoComprobanteVentaId &&
                r.PuntoVenta.Numero == request.PuntoVenta);

            return relacion ?? throw new InvalidOperationException("El punto de venta no tiene habilitada esa configuracion de comprobante.");
        }

        private async Task ValidateTipoComprobanteRequestAsync(TipoComprobanteVentaRequest request, string codigo, int? id)
        {
            NormalizeRequired(codigo, "El codigo es obligatorio.");
            NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            if (request.Signo != 1 && request.Signo != -1) throw new InvalidOperationException("El signo debe ser 1 o -1.");
            if (request.Orden < 0) throw new InvalidOperationException("El orden debe ser mayor o igual a cero.");

            var exists = await _db.TiposComprobanteVenta.AnyAsync(t => t.Codigo == codigo && (!id.HasValue || t.Id != id.Value));
            if (exists) throw new InvalidOperationException("Ya existe una configuracion de comprobante con ese codigo.");
        }

        private async Task ValidatePuntoVentaRequestAsync(PuntoVentaRequest request, int? id)
        {
            if (request.Numero <= 0) throw new InvalidOperationException("El numero de punto de venta debe ser mayor que cero.");
            NormalizeRequired(request.Descripcion, "La descripcion del punto de venta es obligatoria.");

            var exists = await _db.PuntosVenta.AnyAsync(p => p.Numero == request.Numero && (!id.HasValue || p.Id != id.Value));
            if (exists) throw new InvalidOperationException("Ya existe un punto de venta con ese numero.");
        }

        private async Task SyncComprobantesPermitidosAsync(PuntoVenta punto, IEnumerable<int> comprobantesPermitidosIds)
        {
            var selectedIds = comprobantesPermitidosIds.Where(id => id > 0).Distinct().ToList();
            if (selectedIds.Count > 0)
            {
                var activeCount = await _db.TiposComprobanteVenta.CountAsync(t => selectedIds.Contains(t.Id) && t.Activo);
                if (activeCount != selectedIds.Count) throw new InvalidOperationException("Uno o mas comprobantes seleccionados no existen o estan inactivos.");
            }

            var relaciones = await _db.PuntosVentaComprobantes
                .Include(r => r.TipoComprobante)
                .Where(r => r.PuntoVentaId == punto.Id)
                .ToListAsync();

            var user = _userContext.UserName;
            foreach (var selectedId in selectedIds)
            {
                var relacion = relaciones.FirstOrDefault(r => r.TipoComprobanteVentaId == selectedId);
                if (relacion == null)
                {
                    relacion = new PuntoVentaComprobante
                    {
                        PuntoVentaId = punto.Id,
                        PuntoVenta = punto,
                        TipoComprobanteVentaId = selectedId,
                        Activo = true,
                        FechaAlta = DateTime.UtcNow,
                        UsuarioAlta = user
                    };
                    _db.PuntosVentaComprobantes.Add(relacion);
                    relaciones.Add(relacion);
                }
                else if (!relacion.Activo)
                {
                    relacion.Activo = true;
                    relacion.FechaModificacion = DateTime.UtcNow;
                    relacion.UsuarioModificacion = user;
                }
            }

            foreach (var relacion in relaciones.Where(r => !selectedIds.Contains(r.TipoComprobanteVentaId) && r.Activo))
            {
                relacion.Activo = false;
                relacion.FechaModificacion = DateTime.UtcNow;
                relacion.UsuarioModificacion = user;
            }

            await _db.SaveChangesAsync();
            await _db.Entry(punto).Collection(p => p.Comprobantes).Query().Include(r => r.TipoComprobante).LoadAsync();
        }

        private async Task ValidateAlicuotaIvaRequestAsync(AlicuotaIvaVentaRequest request, string codigo, int? id)
        {
            NormalizeRequired(codigo, "El codigo es obligatorio.");
            NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            if (request.Orden < 0) throw new InvalidOperationException("El orden debe ser mayor o igual a cero.");
            if (request.Porcentaje < 0) throw new InvalidOperationException("El porcentaje no puede ser negativo.");
            if (request.TipoTratamiento == TipoTratamientoIvaVenta.Gravado && request.Porcentaje <= 0) throw new InvalidOperationException("Un tratamiento gravado debe tener porcentaje mayor que cero.");
            if (request.TipoTratamiento != TipoTratamientoIvaVenta.Gravado && request.Porcentaje != 0) throw new InvalidOperationException("Un tratamiento exento o no gravado debe tener porcentaje cero.");

            var exists = await _db.AlicuotasIvaVenta.AnyAsync(a => a.Codigo == codigo && (!id.HasValue || a.Id != id.Value));
            if (exists) throw new InvalidOperationException("Ya existe una alicuota de IVA con ese codigo.");
        }

        private async Task ValidateNomencladorFceRequestAsync(NomencladorFceRequest request, string codigo, int? id)
        {
            NormalizeRequired(codigo, "El codigo es obligatorio.");
            NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            if (request.Orden < 0) throw new InvalidOperationException("El orden debe ser mayor o igual a cero.");

            var exists = await _db.NomencladoresFce.AnyAsync(n => n.Codigo == codigo && (!id.HasValue || n.Id != id.Value));
            if (exists) throw new InvalidOperationException("Ya existe un nomenclador FCE con ese codigo.");
        }

        private async Task ValidatePercepcionIibbRequestAsync(PercepcionIibbEntreRiosRequest request, string codigo, int? id)
        {
            NormalizeRequired(codigo, "El codigo es obligatorio.");
            NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            NormalizeRequired(request.NumeroRegimen, "El regimen es obligatorio.");
            if (request.Orden < 0) throw new InvalidOperationException("El orden debe ser mayor o igual a cero.");
            if (request.Porcentaje < 0) throw new InvalidOperationException("El porcentaje no puede ser negativo.");
            if (request.MontoMinimo.HasValue && request.MontoMinimo.Value < 0) throw new InvalidOperationException("El monto minimo no puede ser negativo.");
            if (request.VigenciaDesde == default) throw new InvalidOperationException("La vigencia desde es obligatoria.");
            var desde = NormalizeDateOnlyUtc(request.VigenciaDesde);
            var hasta = request.VigenciaHasta.HasValue ? NormalizeDateOnlyUtc(request.VigenciaHasta.Value) : (DateTime?)null;
            if (hasta.HasValue && hasta.Value < desde) throw new InvalidOperationException("La vigencia hasta no puede ser anterior a la vigencia desde.");

            var jurisdiccion = NormalizeJurisdiccionEntreRios(request.Jurisdiccion);
            var tipoTributo = NormalizeTipoTributoPercepcion(request.TipoTributo);
            var numeroRegimen = NormalizeRequired(request.NumeroRegimen, "El regimen es obligatorio.");

            var exists = await _db.PercepcionesIibbEntreRios.AnyAsync(p => p.Codigo == codigo && (!id.HasValue || p.Id != id.Value));
            if (exists) throw new InvalidOperationException("Ya existe un regimen de percepcion con ese codigo.");

            var overlaps = await _db.PercepcionesIibbEntreRios.AnyAsync(p =>
                (!id.HasValue || p.Id != id.Value) &&
                p.Activo &&
                p.Jurisdiccion == jurisdiccion &&
                p.TipoTributo == tipoTributo &&
                p.NumeroRegimen == numeroRegimen &&
                p.TipoBaseCalculo == request.TipoBaseCalculo &&
                p.VigenciaDesde <= (hasta ?? DateTime.MaxValue) &&
                (!p.VigenciaHasta.HasValue || p.VigenciaHasta.Value >= desde));

            if (overlaps) throw new InvalidOperationException("Ya existe una percepcion equivalente vigente para el periodo indicado.");
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
                    if (cliente != null) clientes[clienteId] = cliente;
                }
            }

            foreach (var obraId in ventas.Select(v => v.ObraExternaId).Distinct())
            {
                if (int.TryParse(obraId, out var parsedId))
                {
                    var obra = await _externalDataService.GetObraByIdAsync(parsedId);
                    if (obra != null) obras[obraId] = obra;
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
                TipoFiscal = tipo.TipoFiscal,
                EsCreditoElectronica = tipo.EsCreditoElectronica,
                EsExportacion = tipo.EsExportacion,
                RequiereNomenclador = tipo.RequiereNomenclador,
                PermiteIva = tipo.PermiteIva,
                Signo = tipo.Signo,
                Activo = tipo.Activo,
                Orden = tipo.Orden,
                FechaAlta = tipo.FechaAlta,
                UsuarioAlta = tipo.UsuarioAlta,
                FechaModificacion = tipo.FechaModificacion,
                UsuarioModificacion = tipo.UsuarioModificacion
            };
        }

        private static PuntoVentaResponse MapPuntoVenta(PuntoVenta punto)
        {
            return new PuntoVentaResponse
            {
                Id = punto.Id,
                Numero = punto.Numero,
                Descripcion = punto.Descripcion,
                Activo = punto.Activo,
                Observaciones = punto.Observaciones,
                ComprobantesPermitidos = punto.Comprobantes
                    .OrderBy(r => r.TipoComprobante.Orden)
                    .ThenBy(r => r.TipoComprobante.Descripcion)
                    .Select(MapPuntoVentaComprobante)
                    .ToList(),
                FechaAlta = punto.FechaAlta,
                UsuarioAlta = punto.UsuarioAlta,
                FechaModificacion = punto.FechaModificacion,
                UsuarioModificacion = punto.UsuarioModificacion
            };
        }

        private static PuntoVentaComprobanteResponse MapPuntoVentaComprobante(PuntoVentaComprobante relacion)
        {
            return new PuntoVentaComprobanteResponse
            {
                Id = relacion.Id,
                PuntoVentaId = relacion.PuntoVentaId,
                PuntoVentaNumero = relacion.PuntoVenta.Numero,
                PuntoVentaDescripcion = relacion.PuntoVenta.Descripcion,
                TipoComprobanteVentaId = relacion.TipoComprobanteVentaId,
                TipoComprobanteCodigo = relacion.TipoComprobante.Codigo,
                TipoComprobanteDescripcion = relacion.TipoComprobante.Descripcion,
                Activo = relacion.Activo,
                Descripcion = relacion.Descripcion,
                FechaAlta = relacion.FechaAlta,
                UsuarioAlta = relacion.UsuarioAlta,
                FechaModificacion = relacion.FechaModificacion,
                UsuarioModificacion = relacion.UsuarioModificacion
            };
        }

        private static AlicuotaIvaVentaResponse MapAlicuotaIva(AlicuotaIvaVenta item)
        {
            return new AlicuotaIvaVentaResponse
            {
                Id = item.Id,
                Codigo = item.Codigo,
                Descripcion = item.Descripcion,
                TipoTratamiento = item.TipoTratamiento,
                Porcentaje = item.Porcentaje,
                Activo = item.Activo,
                Orden = item.Orden,
                FechaAlta = item.FechaAlta,
                UsuarioAlta = item.UsuarioAlta,
                FechaModificacion = item.FechaModificacion,
                UsuarioModificacion = item.UsuarioModificacion
            };
        }

        private static NomencladorFceResponse MapNomencladorFce(NomencladorFce item)
        {
            return new NomencladorFceResponse
            {
                Id = item.Id,
                Codigo = item.Codigo,
                Descripcion = item.Descripcion,
                Activo = item.Activo,
                Orden = item.Orden,
                Observaciones = item.Observaciones,
                FechaAlta = item.FechaAlta,
                UsuarioAlta = item.UsuarioAlta,
                FechaModificacion = item.FechaModificacion,
                UsuarioModificacion = item.UsuarioModificacion
            };
        }

        private static PercepcionIibbEntreRiosResponse MapPercepcionIibb(PercepcionIibbEntreRios item)
        {
            return new PercepcionIibbEntreRiosResponse
            {
                Id = item.Id,
                Codigo = item.Codigo,
                Descripcion = item.Descripcion,
                Jurisdiccion = item.Jurisdiccion,
                TipoTributo = item.TipoTributo,
                NumeroRegimen = item.NumeroRegimen,
                Porcentaje = item.Porcentaje,
                TipoBaseCalculo = item.TipoBaseCalculo,
                MontoMinimo = item.MontoMinimo,
                VigenciaDesde = item.VigenciaDesde,
                VigenciaHasta = item.VigenciaHasta,
                Activo = item.Activo,
                Orden = item.Orden,
                Observaciones = item.Observaciones,
                FechaAlta = item.FechaAlta,
                UsuarioAlta = item.UsuarioAlta,
                FechaModificacion = item.FechaModificacion,
                UsuarioModificacion = item.UsuarioModificacion
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
                PuntoVentaComprobanteId = venta.PuntoVentaComprobanteId,
                TipoComprobanteCodigo = venta.TipoComprobante.Codigo,
                TipoComprobanteDescripcion = venta.TipoComprobante.Descripcion,
                TipoComprobanteLetra = venta.TipoComprobante.Letra,
                TipoComprobanteEsCreditoElectronica = venta.TipoComprobante.EsCreditoElectronica,
                TipoComprobanteEsExportacion = venta.TipoComprobante.EsExportacion,
                TipoComprobanteRequiereNomenclador = venta.TipoComprobante.RequiereNomenclador,
                TipoComprobantePermiteIva = venta.TipoComprobante.PermiteIva,
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

        private static string NormalizeCode(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }

        private static string NormalizeCurrency(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }

        private static string NormalizeRequired(string? value, string error)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(error);
            return value.Trim();
        }

        private static string NormalizeTipoFiscal(TipoComprobanteVentaRequest request)
        {
            if (request.EsExportacion) return "Exportacion";
            return string.IsNullOrWhiteSpace(request.TipoFiscal) ? "Local" : request.TipoFiscal.Trim();
        }

        private static string NormalizeJurisdiccionEntreRios(string? value)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "Entre Rios" : value.Trim();
            if (!normalized.Equals("Entre Rios", StringComparison.OrdinalIgnoreCase) &&
                !normalized.Equals("Entre Ríos", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La jurisdiccion debe ser Entre Rios.");
            }

            return "Entre Rios";
        }

        private static string NormalizeTipoTributoPercepcion(string? value)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "PERCEPCION_IIBB" : value.Trim().ToUpperInvariant();
            if (normalized != "PERCEPCION_IIBB") throw new InvalidOperationException("El tipo de tributo debe ser PERCEPCION_IIBB.");
            return normalized;
        }

        private static int NormalizeSigno(int signo)
        {
            return signo < 0 ? -1 : 1;
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

        private static DateTime NormalizeDateOnlyUtc(DateTime value)
        {
            return new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Utc);
        }

        private static IQueryable<T> ApplySearch<T>(IQueryable<T> query, string? search, params Expression<Func<T, string?>>[] fields)
        {
            if (string.IsNullOrWhiteSpace(search)) return query;
            var term = search.Trim().ToUpperInvariant();
            if (fields.Length == 0) return query;

            Expression? body = null;
            var parameter = Expression.Parameter(typeof(T), "item");
            foreach (var field in fields)
            {
                var replacedBody = new ReplaceParameterVisitor(field.Parameters[0], parameter).Visit(field.Body)!;
                var notNull = Expression.NotEqual(replacedBody, Expression.Constant(null, typeof(string)));
                var toUpper = Expression.Call(replacedBody, nameof(string.ToUpper), Type.EmptyTypes);
                var contains = Expression.Call(toUpper, nameof(string.Contains), Type.EmptyTypes, Expression.Constant(term));
                var condition = Expression.AndAlso(notNull, contains);
                body = body == null ? condition : Expression.OrElse(body, condition);
            }

            return query.Where(Expression.Lambda<Func<T, bool>>(body!, parameter));
        }

        private sealed class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _from;
            private readonly ParameterExpression _to;

            public ReplaceParameterVisitor(ParameterExpression from, ParameterExpression to)
            {
                _from = from;
                _to = to;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _from ? _to : base.VisitParameter(node);
            }
        }

        private sealed record ValidatedVentaRequest(PuntoVentaComprobante Relacion, Client Cliente, Obra Obra);
    }
}
