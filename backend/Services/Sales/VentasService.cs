using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Accounting;
using BudgetControl.Api.DTOs.Sales;
using BudgetControl.Api.Models;
using BudgetControl.Api.Models.Sales;
using BudgetControl.Api.Services.Accounting;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BudgetControl.Api.Services.Sales
{
    public class VentasService : IVentasService
    {
        private const string MonedaBase = "ARS";
        private const int MaxPageSize = 100;
        private const string CodigoOperacionFacturaVenta = "FACTURA_VENTA";
        private const string ModuloOrigenVentas = "VENTAS";
        private const string TipoMovimientoCuentaFactura = "FACTURA";

        private readonly AppDbContext _db;
        private readonly IExternalDataService _externalDataService;
        private readonly IUserContext _userContext;
        private readonly ICalculadorVentasService _calculador;
        private readonly IContabilizacionAutomaticaService _contabilizacionAutomatica;
        private readonly IConfiguracionesContablesService _configuracionesContables;

        public VentasService(
            AppDbContext db,
            IExternalDataService externalDataService,
            IUserContext userContext,
            ICalculadorVentasService calculador,
            IContabilizacionAutomaticaService contabilizacionAutomatica,
            IConfiguracionesContablesService configuracionesContables)
        {
            _db = db;
            _externalDataService = externalDataService;
            _userContext = userContext;
            _calculador = calculador;
            _contabilizacionAutomatica = contabilizacionAutomatica;
            _configuracionesContables = configuracionesContables;
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

        public async Task<IEnumerable<PuntoVentaSelectorResponse>> GetPuntosVentaPorComprobanteAsync(int tipoComprobanteVentaId, int? relacionActualId = null)
        {
            var tipo = await _db.TiposComprobanteVenta.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tipoComprobanteVentaId);
            if (tipo == null) throw new InvalidOperationException("Configuracion de comprobante no encontrada.");
            if (!tipo.Activo) throw new InvalidOperationException("La configuracion de comprobante se encuentra inactiva.");

            var query = GetPuntoVentaComprobanteQuery()
                .Where(r => r.TipoComprobanteVentaId == tipoComprobanteVentaId);

            if (relacionActualId.HasValue)
            {
                query = query.Where(r =>
                    (r.Activo && r.PuntoVenta.Activo && r.TipoComprobante.Activo) ||
                    r.Id == relacionActualId.Value);
            }
            else
            {
                query = query.Where(r => r.Activo && r.PuntoVenta.Activo && r.TipoComprobante.Activo);
            }

            var relaciones = await query
                .OrderBy(r => r.PuntoVenta.Numero)
                .ThenBy(r => r.PuntoVenta.Descripcion)
                .ToListAsync();

            return relaciones.Select(MapPuntoVentaSelector);
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

        public async Task<IEnumerable<CategoriaItemFacturableResponse>> GetCategoriasItemsFacturablesAsync(bool soloActivos = false, string? search = null)
        {
            var query = _db.CategoriasItemsFacturables.AsNoTracking();
            if (soloActivos) query = query.Where(c => c.Activo);
            query = ApplySearch(query, search, c => c.Codigo, c => c.Descripcion);

            var items = await query.OrderBy(c => c.Orden).ThenBy(c => c.Descripcion).ToListAsync();
            return items.Select(MapCategoriaItemFacturable);
        }

        public async Task<CategoriaItemFacturableResponse?> GetCategoriaItemFacturableAsync(int id)
        {
            var item = await _db.CategoriasItemsFacturables.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            return item == null ? null : MapCategoriaItemFacturable(item);
        }

        public async Task<CategoriaItemFacturableResponse> CreateCategoriaItemFacturableAsync(CategoriaItemFacturableRequest request)
        {
            var codigo = NormalizeCode(request.Codigo);
            await ValidateCategoriaItemFacturableRequestAsync(request, codigo, null);

            var item = new CategoriaItemFacturable
            {
                Codigo = codigo,
                Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria."),
                Activo = request.Activo,
                Orden = request.Orden,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            _db.CategoriasItemsFacturables.Add(item);
            await _db.SaveChangesAsync();
            return MapCategoriaItemFacturable(item);
        }

        public async Task<CategoriaItemFacturableResponse> UpdateCategoriaItemFacturableAsync(int id, CategoriaItemFacturableRequest request)
        {
            var item = await _db.CategoriasItemsFacturables.FirstOrDefaultAsync(c => c.Id == id);
            if (item == null) throw new InvalidOperationException("Categoria de item facturable no encontrada.");

            var codigo = NormalizeCode(request.Codigo);
            await ValidateCategoriaItemFacturableRequestAsync(request, codigo, id);

            item.Codigo = codigo;
            item.Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            item.Activo = request.Activo;
            item.Orden = request.Orden;
            item.FechaModificacion = DateTime.UtcNow;
            item.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            return MapCategoriaItemFacturable(item);
        }

        public async Task<IEnumerable<UnidadMedidaVentaResponse>> GetUnidadesMedidaAsync(bool soloActivos = false, string? search = null)
        {
            var query = _db.UnidadesMedidaVenta.AsNoTracking();
            if (soloActivos) query = query.Where(u => u.Activo);
            query = ApplySearch(query, search, u => u.Codigo, u => u.Descripcion, u => u.Abreviatura);

            var items = await query.OrderBy(u => u.Orden).ThenBy(u => u.Descripcion).ToListAsync();
            return items.Select(MapUnidadMedida);
        }

        public async Task<UnidadMedidaVentaResponse?> GetUnidadMedidaAsync(int id)
        {
            var item = await _db.UnidadesMedidaVenta.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            return item == null ? null : MapUnidadMedida(item);
        }

        public async Task<UnidadMedidaVentaResponse> CreateUnidadMedidaAsync(UnidadMedidaVentaRequest request)
        {
            var codigo = NormalizeCode(request.Codigo);
            await ValidateUnidadMedidaRequestAsync(request, codigo, null);

            var item = new UnidadMedidaVenta
            {
                Codigo = codigo,
                Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria."),
                Abreviatura = NormalizeOptional(request.Abreviatura),
                PermiteDecimales = request.PermiteDecimales,
                Activo = request.Activo,
                Orden = request.Orden,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            _db.UnidadesMedidaVenta.Add(item);
            await _db.SaveChangesAsync();
            return MapUnidadMedida(item);
        }

        public async Task<UnidadMedidaVentaResponse> UpdateUnidadMedidaAsync(int id, UnidadMedidaVentaRequest request)
        {
            var item = await _db.UnidadesMedidaVenta.FirstOrDefaultAsync(u => u.Id == id);
            if (item == null) throw new InvalidOperationException("Unidad de medida no encontrada.");

            var codigo = NormalizeCode(request.Codigo);
            await ValidateUnidadMedidaRequestAsync(request, codigo, id);

            item.Codigo = codigo;
            item.Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            item.Abreviatura = NormalizeOptional(request.Abreviatura);
            item.PermiteDecimales = request.PermiteDecimales;
            item.Activo = request.Activo;
            item.Orden = request.Orden;
            item.FechaModificacion = DateTime.UtcNow;
            item.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            return MapUnidadMedida(item);
        }

        public async Task<IEnumerable<ItemFacturableResponse>> GetItemsFacturablesAsync(bool soloActivos = false, string? search = null, int? categoriaId = null, int? unidadMedidaId = null, int? tratamientoIvaId = null, int? nomencladorId = null)
        {
            var query = GetItemsFacturablesQuery();
            if (soloActivos) query = query.Where(i => i.Activo);
            if (categoriaId.HasValue) query = query.Where(i => i.CategoriaItemFacturableId == categoriaId.Value);
            if (unidadMedidaId.HasValue) query = query.Where(i => i.UnidadMedidaVentaId == unidadMedidaId.Value);
            if (tratamientoIvaId.HasValue) query = query.Where(i => i.TratamientoIvaPredeterminadoId == tratamientoIvaId.Value);
            if (nomencladorId.HasValue) query = query.Where(i => i.NomencladorPredeterminadoId == nomencladorId.Value);
            query = ApplySearch(query, search, i => i.Codigo, i => i.Descripcion, i => i.DescripcionAmpliada);

            var items = await query.OrderBy(i => i.Orden).ThenBy(i => i.Descripcion).ToListAsync();
            return items.Select(MapItemFacturable);
        }

        public async Task<ItemFacturableResponse?> GetItemFacturableAsync(int id)
        {
            var item = await GetItemsFacturablesQuery().FirstOrDefaultAsync(i => i.Id == id);
            return item == null ? null : MapItemFacturable(item);
        }

        public async Task<ItemFacturableResponse> CreateItemFacturableAsync(ItemFacturableRequest request)
        {
            var codigo = NormalizeCode(request.Codigo);
            var normalized = await ValidateItemFacturableRequestAsync(request, codigo, null);

            var item = new ItemFacturable
            {
                Codigo = codigo,
                Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria."),
                DescripcionAmpliada = NormalizeOptional(request.DescripcionAmpliada),
                CategoriaItemFacturableId = normalized.Categoria?.Id,
                UnidadMedidaVentaId = normalized.Unidad.Id,
                TratamientoIvaPredeterminadoId = normalized.TratamientoIva.Id,
                NomencladorPredeterminadoId = normalized.Nomenclador?.Id,
                PrecioPredeterminado = request.PrecioPredeterminado,
                Activo = request.Activo,
                Orden = request.Orden,
                Observaciones = NormalizeOptional(request.Observaciones),
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            _db.ItemsFacturables.Add(item);
            await _db.SaveChangesAsync();
            item.Categoria = normalized.Categoria;
            item.UnidadMedida = normalized.Unidad;
            item.TratamientoIvaPredeterminado = normalized.TratamientoIva;
            item.NomencladorPredeterminado = normalized.Nomenclador;
            return MapItemFacturable(item);
        }

        public async Task<ItemFacturableResponse> UpdateItemFacturableAsync(int id, ItemFacturableRequest request)
        {
            var item = await GetItemsFacturablesQuery(false).FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) throw new InvalidOperationException("Item facturable no encontrado.");

            var codigo = NormalizeCode(request.Codigo);
            var normalized = await ValidateItemFacturableRequestAsync(request, codigo, id);

            item.Codigo = codigo;
            item.Descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            item.DescripcionAmpliada = NormalizeOptional(request.DescripcionAmpliada);
            item.CategoriaItemFacturableId = normalized.Categoria?.Id;
            item.Categoria = normalized.Categoria;
            item.UnidadMedidaVentaId = normalized.Unidad.Id;
            item.UnidadMedida = normalized.Unidad;
            item.TratamientoIvaPredeterminadoId = normalized.TratamientoIva.Id;
            item.TratamientoIvaPredeterminado = normalized.TratamientoIva;
            item.NomencladorPredeterminadoId = normalized.Nomenclador?.Id;
            item.NomencladorPredeterminado = normalized.Nomenclador;
            item.PrecioPredeterminado = request.PrecioPredeterminado;
            item.Activo = request.Activo;
            item.Orden = request.Orden;
            item.Observaciones = NormalizeOptional(request.Observaciones);
            item.FechaModificacion = DateTime.UtcNow;
            item.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            return MapItemFacturable(item);
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
            if (venta.Detalles.Any() && normalized.Relacion.Id != venta.PuntoVentaComprobanteId)
            {
                throw new InvalidOperationException("No se puede cambiar la configuracion de comprobante o punto de venta porque la venta ya posee detalles.");
            }

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
            MarkPercepcionPendiente(venta);
            venta.FechaModificacion = DateTime.UtcNow;
            venta.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();

            venta.TipoComprobante = normalized.Relacion.TipoComprobante;
            venta.PuntoVentaComprobante = normalized.Relacion;
            return MapVenta(venta, new Dictionary<string, Client> { [venta.ClienteExternoId] = normalized.Cliente }, new Dictionary<string, Obra> { [venta.ObraExternaId] = normalized.Obra });
        }

        public async Task<IEnumerable<VentaDetalleResponse>> GetDetallesAsync(int ventaId)
        {
            var exists = await _db.Ventas.AnyAsync(v => v.Id == ventaId);
            if (!exists) throw new InvalidOperationException("Venta no encontrada.");

            var detalles = await GetDetalleQuery()
                .Where(d => d.VentaId == ventaId)
                .OrderBy(d => d.NumeroLinea)
                .ToListAsync();

            return detalles.Select(MapDetalle);
        }

        public async Task<VentaDetalleMutationResponse> CreateDetalleAsync(int ventaId, VentaDetalleRequest request)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var venta = await GetVentaForDetalleAsync(ventaId);
            EnsureBorrador(venta);

            var normalized = await ValidateDetalleRequestAsync(venta, request);
            var nextLine = venta.Detalles.Any() ? venta.Detalles.Max(d => d.NumeroLinea) + 1 : 1;
            var detalle = BuildDetalle(venta.Id, nextLine, request, normalized);
            detalle.FechaAlta = DateTime.UtcNow;
            detalle.UsuarioAlta = _userContext.UserName;

            _db.VentasDetalle.Add(detalle);
            if (!venta.Detalles.Any(d => ReferenceEquals(d, detalle)))
            {
                venta.Detalles.Add(detalle);
            }
            MarkPercepcionPendiente(venta);
            _calculador.RecalcularTotales(venta);
            venta.FechaModificacion = DateTime.UtcNow;
            venta.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new VentaDetalleMutationResponse
            {
                Detalle = MapDetalle(detalle),
                Venta = await BuildVentaResponseAsync(venta)
            };
        }

        public async Task<VentaDetalleMutationResponse> UpdateDetalleAsync(int ventaId, int detalleId, VentaDetalleRequest request)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var venta = await GetVentaForDetalleAsync(ventaId);
            EnsureBorrador(venta);

            var detalle = venta.Detalles.FirstOrDefault(d => d.Id == detalleId);
            if (detalle == null) throw new InvalidOperationException("Detalle no encontrado para la venta indicada.");

            var normalized = await ValidateDetalleRequestAsync(venta, request);
            ApplyDetalle(detalle, request, normalized);
            detalle.FechaModificacion = DateTime.UtcNow;
            detalle.UsuarioModificacion = _userContext.UserName;

            MarkPercepcionPendiente(venta);
            _calculador.RecalcularTotales(venta);
            venta.FechaModificacion = DateTime.UtcNow;
            venta.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new VentaDetalleMutationResponse
            {
                Detalle = MapDetalle(detalle),
                Venta = await BuildVentaResponseAsync(venta)
            };
        }

        public async Task<VentaResponse> DeleteDetalleAsync(int ventaId, int detalleId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var venta = await GetVentaForDetalleAsync(ventaId);
            EnsureBorrador(venta);

            var detalle = venta.Detalles.FirstOrDefault(d => d.Id == detalleId);
            if (detalle == null) throw new InvalidOperationException("Detalle no encontrado para la venta indicada.");

            _db.VentasDetalle.Remove(detalle);
            venta.Detalles.Remove(detalle);
            MarkPercepcionPendiente(venta);
            _calculador.RecalcularTotales(venta);
            venta.FechaModificacion = DateTime.UtcNow;
            venta.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return await BuildVentaResponseAsync(venta);
        }

        public async Task<VentaConfirmacionValidacionResponse> ValidarConfirmacionAsync(int ventaId)
        {
            var venta = await GetVentaQuery(false).FirstOrDefaultAsync(v => v.Id == ventaId);
            if (venta == null)
            {
                return BuildValidationResult(null, new List<string> { "Venta no encontrada." }, new List<string>());
            }

            return await ValidateConfirmacionAsync(venta);
        }

        public async Task<VentaConfirmacionResponse> ConfirmarVentaAsync(int ventaId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var venta = await GetVentaQuery(false).FirstOrDefaultAsync(v => v.Id == ventaId);
            if (venta == null) throw new InvalidOperationException("Venta no encontrada.");
            if (venta.Estado == VentaEstado.Confirmada) throw new InvalidOperationException("La factura ya fue confirmada.");
            if (venta.Estado != VentaEstado.Borrador) throw new InvalidOperationException("Solo una factura Borrador puede confirmarse.");

            var validacion = await ValidateConfirmacionAsync(venta);
            if (!validacion.EsValida)
            {
                throw new InvalidOperationException(validacion.Errores.First());
            }

            _calculador.RecalcularTotales(venta);
            var solicitud = BuildSolicitudContable(venta);
            await EnsureMovimientoCuentaCorrienteAsync(venta);

            var asiento = await _contabilizacionAutomatica.GenerarAsientoAutomaticoAsync(solicitud);
            var now = DateTime.UtcNow;
            venta.Estado = VentaEstado.Confirmada;
            venta.FechaConfirmacion = now;
            venta.UsuarioConfirmacion = _userContext.UserName;
            venta.AsientoContableId = asiento.AsientoContableId;
            venta.FechaModificacion = now;
            venta.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new VentaConfirmacionResponse
            {
                Venta = await BuildVentaResponseAsync(venta),
                AsientoContableId = asiento.AsientoContableId,
                AsientoYaExistia = asiento.YaExistia,
                TotalFinal = venta.Total,
                ImporteAsociadoPlan = GetImporteAsociadoPlan(venta),
                CantidadObligacionesAplicadas = 0,
                CodigoOperacionContable = CodigoOperacionFacturaVenta
            };
        }

        private async Task<VentaConfirmacionValidacionResponse> ValidateConfirmacionAsync(Venta venta)
        {
            var errores = new List<string>();
            var advertencias = new List<string>();

            if (venta.Estado == VentaEstado.Confirmada)
            {
                errores.Add("La factura ya fue confirmada.");
            }
            else if (venta.Estado != VentaEstado.Borrador)
            {
                errores.Add("Solo una factura Borrador puede confirmarse.");
            }

            try
            {
                await ValidateRequestAsync(new VentaHeaderRequest
                {
                    TipoComprobanteVentaId = venta.TipoComprobanteVentaId,
                    PuntoVentaComprobanteId = venta.PuntoVentaComprobanteId,
                    ClienteExternoId = venta.ClienteExternoId,
                    ObraExternaId = venta.ObraExternaId,
                    FechaComprobante = venta.FechaComprobante,
                    PuntoVenta = venta.PuntoVenta,
                    NumeroComprobante = venta.NumeroComprobante,
                    MonedaCodigo = venta.MonedaCodigo,
                    Cotizacion = venta.Cotizacion,
                    Observaciones = venta.Observaciones
                }, venta.Id);
            }
            catch (InvalidOperationException ex)
            {
                errores.Add(ex.Message);
            }

            if (venta.Detalles == null || !venta.Detalles.Any())
            {
                errores.Add("La factura no posee detalles.");
            }
            else
            {
                await ValidateDetallesConfirmacionAsync(venta, errores);
            }

            ValidateTotalesConfirmacion(venta, errores);
            ValidatePercepcionConfirmacion(venta, errores, advertencias);
            await ValidateConfiguracionContableConfirmacionAsync(venta, errores);

            return BuildValidationResult(venta, errores, advertencias);
        }

        private async Task ValidateDetallesConfirmacionAsync(Venta venta, ICollection<string> errores)
        {
            var itemIds = venta.Detalles
                .Where(d => d.ItemFacturableId.HasValue)
                .Select(d => d.ItemFacturableId!.Value)
                .Distinct()
                .ToList();

            var items = await GetItemsFacturablesQuery()
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);

            foreach (var detalle in venta.Detalles.OrderBy(d => d.NumeroLinea))
            {
                var linea = $"Linea {detalle.NumeroLinea}:";
                if (!detalle.ItemFacturableId.HasValue)
                {
                    errores.Add($"{linea} debe tener item facturable.");
                }
                else if (!items.TryGetValue(detalle.ItemFacturableId.Value, out var item))
                {
                    errores.Add($"{linea} el item facturable no existe.");
                }
                else
                {
                    if (!item.Activo) errores.Add($"{linea} el item facturable se encuentra inactivo.");
                    if (!item.UnidadMedida.Activo) errores.Add($"{linea} la unidad de medida del item se encuentra inactiva.");
                }

                if (detalle.Cantidad <= 0) errores.Add($"{linea} la cantidad debe ser mayor que cero.");
                if (detalle.PrecioUnitario < 0) errores.Add($"{linea} el precio unitario no puede ser negativo.");
                if (detalle.PorcentajeDescuento < 0 || detalle.PorcentajeDescuento > 100) errores.Add($"{linea} el descuento debe estar entre 0 y 100.");
                if (venta.TipoComprobante.RequiereNomenclador && !detalle.NomencladorId.HasValue) errores.Add($"{linea} el comprobante requiere nomenclador.");
                if (venta.TipoComprobante.EsExportacion && detalle.TipoTratamientoIva == TipoTratamientoIvaVenta.Gravado)
                {
                    errores.Add($"{linea} un comprobante de exportacion no permite tratamiento de IVA gravado local.");
                }
            }
        }

        private void ValidateTotalesConfirmacion(Venta venta, ICollection<string> errores)
        {
            var subtotalBruto = venta.SubtotalBruto;
            var totalDescuentos = venta.TotalDescuentos;
            var netoGravado = venta.NetoGravado;
            var totalExento = venta.TotalExento;
            var totalNoGravado = venta.TotalNoGravado;
            var totalIva = venta.TotalIva;
            var totalAntesPercepciones = venta.TotalAntesPercepciones;
            var total = venta.Total;

            _calculador.RecalcularTotales(venta);

            if (subtotalBruto != venta.SubtotalBruto ||
                totalDescuentos != venta.TotalDescuentos ||
                netoGravado != venta.NetoGravado ||
                totalExento != venta.TotalExento ||
                totalNoGravado != venta.TotalNoGravado ||
                totalIva != venta.TotalIva ||
                totalAntesPercepciones != venta.TotalAntesPercepciones ||
                total != venta.Total)
            {
                errores.Add("Los totales de la factura no coinciden con el recalculo actual.");
            }
        }

        private static void ValidatePercepcionConfirmacion(Venta venta, ICollection<string> errores, ICollection<string> advertencias)
        {
            if (venta.PercepcionIibbRequiereRecalculo)
            {
                errores.Add("La percepcion de Ingresos Brutos requiere recalculo.");
            }

            var percepcionesActivas = venta.PercepcionesIibb?.Where(p => p.Activa).ToList() ?? new List<VentaPercepcionIibb>();
            var totalPercepciones = RoundMoney(percepcionesActivas.Sum(p => p.Importe));
            if (venta.TotalPercepciones > 0 && !percepcionesActivas.Any())
            {
                errores.Add("La percepcion de Ingresos Brutos no se encuentra calculada.");
            }

            if (venta.TotalPercepciones != totalPercepciones)
            {
                errores.Add("La percepcion de Ingresos Brutos no coincide con los totales actuales.");
            }

            if (venta.TotalPercepciones <= 0)
            {
                var motivo = percepcionesActivas.FirstOrDefault()?.Motivo;
                advertencias.Add(string.IsNullOrWhiteSpace(motivo)
                    ? "La factura no posee percepcion de Ingresos Brutos aplicada."
                    : $"La factura no posee percepcion aplicada: {motivo}");
            }
        }

        private async Task ValidateConfiguracionContableConfirmacionAsync(Venta venta, ICollection<string> errores)
        {
            var configuracion = await _configuracionesContables.GetConfiguracionPorOperacionAsync(CodigoOperacionFacturaVenta);
            if (configuracion == null || !configuracion.Activa)
            {
                errores.Add("La configuracion contable para la operacion FACTURA_VENTA no existe o esta inactiva.");
                return;
            }

            var conceptos = configuracion.Detalles.Select(d => d.Concepto).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var conceptosObligatorios = configuracion.Detalles
                .Where(d => d.EsObligatorio)
                .Select(d => d.Concepto)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var requerido in new[] { "CLIENTES", "VENTA_NETA" })
            {
                if (!conceptos.Contains(requerido))
                {
                    errores.Add($"La configuracion contable FACTURA_VENTA no posee el concepto {requerido}.");
                }
            }

            if (venta.TotalIva > 0 && !conceptos.Contains("IVA_DEBITO"))
            {
                errores.Add("La configuracion contable FACTURA_VENTA no posee el concepto IVA_DEBITO requerido por el IVA de la factura.");
            }

            if (venta.TotalIva <= 0 && conceptosObligatorios.Contains("IVA_DEBITO"))
            {
                errores.Add("La configuracion contable FACTURA_VENTA exige IVA_DEBITO, pero la factura no posee IVA.");
            }

            if (venta.TotalPercepciones > 0 && !conceptos.Contains("PERCEPCION_IIBB"))
            {
                errores.Add("La configuracion contable FACTURA_VENTA no posee el concepto PERCEPCION_IIBB requerido por la percepcion aplicada.");
            }

            if (venta.TotalPercepciones <= 0 && conceptosObligatorios.Contains("PERCEPCION_IIBB"))
            {
                errores.Add("La configuracion contable FACTURA_VENTA exige PERCEPCION_IIBB, pero la factura no posee percepcion aplicada.");
            }
        }

        private async Task EnsureMovimientoCuentaCorrienteAsync(Venta venta)
        {
            var idOrigen = venta.Id.ToString();
            var exists = await _db.VentasMovimientosCuentaCorriente.AnyAsync(m =>
                m.ModuloOrigen == ModuloOrigenVentas &&
                m.IdOrigen == idOrigen &&
                m.TipoMovimiento == TipoMovimientoCuentaFactura);

            if (exists) return;

            _db.VentasMovimientosCuentaCorriente.Add(new VentaMovimientoCuentaCorriente
            {
                ClienteExternoId = venta.ClienteExternoId,
                ObraExternaId = venta.ObraExternaId,
                Fecha = venta.FechaComprobante,
                TipoMovimiento = TipoMovimientoCuentaFactura,
                Debe = venta.Total,
                Haber = 0,
                ModuloOrigen = ModuloOrigenVentas,
                IdOrigen = idOrigen,
                Descripcion = $"Factura {venta.PuntoVenta:0000}-{venta.NumeroComprobante:00000000}",
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            });
        }

        private static SolicitudContabilizacionAutomaticaRequest BuildSolicitudContable(Venta venta)
        {
            var importes = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["CLIENTES"] = venta.Total,
                ["VENTA_NETA"] = GetImporteAsociadoPlan(venta)
            };

            if (venta.TotalIva > 0)
            {
                importes["IVA_DEBITO"] = venta.TotalIva;
            }

            if (venta.TotalPercepciones > 0)
            {
                importes["PERCEPCION_IIBB"] = venta.TotalPercepciones;
            }

            return new SolicitudContabilizacionAutomaticaRequest
            {
                CodigoOperacion = CodigoOperacionFacturaVenta,
                ModuloOrigen = ModuloOrigenVentas,
                IdOrigen = venta.Id.ToString(),
                Fecha = venta.FechaComprobante,
                Descripcion = $"Factura de venta {venta.PuntoVenta:0000}-{venta.NumeroComprobante:00000000}",
                ImportesPorConcepto = importes
            };
        }

        private static VentaConfirmacionValidacionResponse BuildValidationResult(Venta? venta, List<string> errores, List<string> advertencias)
        {
            return new VentaConfirmacionValidacionResponse
            {
                EsValida = errores.Count == 0,
                Errores = errores.Distinct().ToList(),
                Advertencias = advertencias.Distinct().ToList(),
                TotalFinal = venta?.Total ?? 0,
                ImporteAsociadoPlan = venta == null ? 0 : GetImporteAsociadoPlan(venta),
                CantidadObligacionesAplicadas = 0,
                CodigoOperacionContable = CodigoOperacionFacturaVenta,
                ConceptosContables = BuildSolicitudConceptos(venta)
            };
        }

        private static List<string> BuildSolicitudConceptos(Venta? venta)
        {
            var conceptos = new List<string> { "CLIENTES", "VENTA_NETA" };
            if ((venta?.TotalIva ?? 0) > 0)
            {
                conceptos.Add("IVA_DEBITO");
            }

            if ((venta?.TotalPercepciones ?? 0) > 0)
            {
                conceptos.Add("PERCEPCION_IIBB");
            }

            return conceptos;
        }

        private static decimal GetImporteAsociadoPlan(Venta venta)
        {
            return RoundMoney(venta.TotalAntesPercepciones - venta.TotalIva);
        }

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private IQueryable<Venta> GetVentaQuery(bool asNoTracking = true)
        {
            var query = _db.Ventas
                .Include(v => v.TipoComprobante)
                .Include(v => v.PuntoVentaComprobante)
                    .ThenInclude(r => r!.PuntoVenta)
                .Include(v => v.PuntoVentaComprobante)
                    .ThenInclude(r => r!.TipoComprobante)
                .Include(v => v.Detalles)
                .Include(v => v.PercepcionesIibb);

            return asNoTracking ? query.AsNoTracking() : query;
        }

        private IQueryable<VentaDetalle> GetDetalleQuery()
        {
            return _db.VentasDetalle
                .AsNoTracking()
                .Include(d => d.ItemFacturable)
                .Include(d => d.CategoriaItemFacturable)
                .Include(d => d.UnidadMedida)
                .Include(d => d.TratamientoIva)
                .Include(d => d.Nomenclador);
        }

        private IQueryable<ItemFacturable> GetItemsFacturablesQuery(bool asNoTracking = true)
        {
            var query = _db.ItemsFacturables
                .Include(i => i.Categoria)
                .Include(i => i.UnidadMedida)
                .Include(i => i.TratamientoIvaPredeterminado)
                .Include(i => i.NomencladorPredeterminado);

            return asNoTracking ? query.AsNoTracking() : query;
        }

        private async Task<Venta> GetVentaForDetalleAsync(int ventaId)
        {
            var venta = await _db.Ventas
                .Include(v => v.TipoComprobante)
                .Include(v => v.PuntoVentaComprobante)
                    .ThenInclude(r => r!.PuntoVenta)
                .Include(v => v.PuntoVentaComprobante)
                    .ThenInclude(r => r!.TipoComprobante)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.ItemFacturable)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.CategoriaItemFacturable)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.UnidadMedida)
                .Include(v => v.PercepcionesIibb)
                .FirstOrDefaultAsync(v => v.Id == ventaId);

            return venta ?? throw new InvalidOperationException("Venta no encontrada.");
        }

        private static void EnsureBorrador(Venta venta)
        {
            if (venta.Estado != VentaEstado.Borrador) throw new InvalidOperationException("Solo una venta en estado Borrador permite modificar detalles.");
        }

        private async Task<ValidatedDetalleRequest> ValidateDetalleRequestAsync(Venta venta, VentaDetalleRequest request)
        {
            var descripcion = NormalizeRequired(request.Descripcion, "La descripcion del detalle es obligatoria.");
            if (request.Cantidad <= 0) throw new InvalidOperationException("La cantidad debe ser mayor que cero.");
            if (request.PrecioUnitario < 0) throw new InvalidOperationException("El precio unitario no puede ser negativo.");
            if (request.PorcentajeDescuento < 0 || request.PorcentajeDescuento > 100) throw new InvalidOperationException("El descuento debe estar entre 0 y 100.");

            if (!request.ItemFacturableId.HasValue) throw new InvalidOperationException("El item facturable es obligatorio para nuevos detalles.");
            var item = await GetItemsFacturablesQuery(false).FirstOrDefaultAsync(i => i.Id == request.ItemFacturableId.Value);
            if (item == null) throw new InvalidOperationException("Item facturable no encontrado.");
            if (!item.Activo) throw new InvalidOperationException("El item facturable se encuentra inactivo.");
            if (!item.UnidadMedida.Activo) throw new InvalidOperationException("La unidad de medida del item se encuentra inactiva.");

            var tratamiento = await _db.AlicuotasIvaVenta.FirstOrDefaultAsync(a => a.Id == request.TratamientoIvaId);
            if (tratamiento == null) throw new InvalidOperationException("Tratamiento de IVA no encontrado.");
            if (!tratamiento.Activo) throw new InvalidOperationException("El tratamiento de IVA se encuentra inactivo.");
            if (venta.TipoComprobante.EsExportacion && tratamiento.TipoTratamiento == TipoTratamientoIvaVenta.Gravado)
            {
                throw new InvalidOperationException("Un comprobante de exportacion no permite tratamiento de IVA gravado local.");
            }

            NomencladorFce? nomenclador = null;
            if (venta.TipoComprobante.RequiereNomenclador)
            {
                if (!request.NomencladorId.HasValue) throw new InvalidOperationException("El comprobante requiere nomenclador en cada detalle.");
                nomenclador = await _db.NomencladoresFce.FirstOrDefaultAsync(n => n.Id == request.NomencladorId.Value);
                if (nomenclador == null) throw new InvalidOperationException("Nomenclador no encontrado.");
                if (!nomenclador.Activo) throw new InvalidOperationException("El nomenclador se encuentra inactivo.");
            }
            else if (request.NomencladorId.HasValue)
            {
                nomenclador = await _db.NomencladoresFce.FirstOrDefaultAsync(n => n.Id == request.NomencladorId.Value);
                if (nomenclador == null) throw new InvalidOperationException("Nomenclador no encontrado.");
                if (!nomenclador.Activo) throw new InvalidOperationException("El nomenclador se encuentra inactivo.");
            }

            var calculo = _calculador.CalcularDetalle(request.Cantidad, request.PrecioUnitario, request.PorcentajeDescuento, venta.TipoComprobante, tratamiento);
            return new ValidatedDetalleRequest(descripcion, item, tratamiento, nomenclador, calculo);
        }

        private VentaDetalle BuildDetalle(int ventaId, int numeroLinea, VentaDetalleRequest request, ValidatedDetalleRequest validated)
        {
            var detalle = new VentaDetalle
            {
                VentaId = ventaId,
                NumeroLinea = numeroLinea
            };
            ApplyDetalle(detalle, request, validated);
            return detalle;
        }

        private static void ApplyDetalle(VentaDetalle detalle, VentaDetalleRequest request, ValidatedDetalleRequest validated)
        {
            detalle.ItemFacturableId = validated.ItemFacturable.Id;
            detalle.ItemFacturable = validated.ItemFacturable;
            detalle.CodigoItem = validated.ItemFacturable.Codigo;
            detalle.ItemFacturableDescripcion = validated.ItemFacturable.Descripcion;
            detalle.CategoriaItemFacturableId = validated.ItemFacturable.Categoria?.Id;
            detalle.CategoriaItemFacturable = validated.ItemFacturable.Categoria;
            detalle.CategoriaItemFacturableCodigo = validated.ItemFacturable.Categoria?.Codigo;
            detalle.CategoriaItemFacturableDescripcion = validated.ItemFacturable.Categoria?.Descripcion;
            detalle.UnidadMedidaVentaId = validated.ItemFacturable.UnidadMedida.Id;
            detalle.UnidadMedida = validated.ItemFacturable.UnidadMedida;
            detalle.UnidadMedidaCodigo = validated.ItemFacturable.UnidadMedida.Codigo;
            detalle.UnidadMedidaDescripcion = validated.ItemFacturable.UnidadMedida.Descripcion;
            detalle.UnidadMedidaAbreviatura = validated.ItemFacturable.UnidadMedida.Abreviatura;
            detalle.Descripcion = validated.Descripcion;
            detalle.Cantidad = request.Cantidad;
            detalle.PrecioUnitario = request.PrecioUnitario;
            detalle.PorcentajeDescuento = request.PorcentajeDescuento;
            detalle.ImporteBruto = validated.Calculo.ImporteBruto;
            detalle.ImporteDescuento = validated.Calculo.ImporteDescuento;
            detalle.Neto = validated.Calculo.Neto;
            detalle.TratamientoIvaId = validated.TratamientoIva.Id;
            detalle.TratamientoIva = validated.TratamientoIva;
            detalle.TratamientoIvaCodigo = validated.TratamientoIva.Codigo;
            detalle.TratamientoIvaDescripcion = validated.TratamientoIva.Descripcion;
            detalle.TipoTratamientoIva = validated.TratamientoIva.TipoTratamiento;
            detalle.PorcentajeIvaAplicado = validated.Calculo.PorcentajeIvaAplicado;
            detalle.ImporteIva = validated.Calculo.ImporteIva;
            detalle.NomencladorId = validated.Nomenclador?.Id;
            detalle.Nomenclador = validated.Nomenclador;
            detalle.NomencladorCodigo = validated.Nomenclador?.Codigo;
            detalle.NomencladorDescripcion = validated.Nomenclador?.Descripcion;
            detalle.TotalLinea = validated.Calculo.TotalLinea;
            detalle.Observaciones = NormalizeOptional(request.Observaciones);
        }

        private async Task<VentaResponse> BuildVentaResponseAsync(Venta venta)
        {
            var lookups = await LoadLookupsAsync(new[] { venta });
            return MapVenta(venta, lookups.Clientes, lookups.Obras);
        }

        private static void MarkPercepcionPendiente(Venta venta)
        {
            if (venta.PercepcionesIibb.Any(p => p.Activa))
            {
                venta.PercepcionIibbRequiereRecalculo = true;
            }
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

        private async Task ValidateCategoriaItemFacturableRequestAsync(CategoriaItemFacturableRequest request, string codigo, int? id)
        {
            NormalizeRequired(codigo, "El codigo es obligatorio.");
            NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            if (request.Orden < 0) throw new InvalidOperationException("El orden debe ser mayor o igual a cero.");

            var exists = await _db.CategoriasItemsFacturables.AnyAsync(c => c.Codigo == codigo && (!id.HasValue || c.Id != id.Value));
            if (exists) throw new InvalidOperationException("Ya existe una categoria de item con ese codigo.");
        }

        private async Task ValidateUnidadMedidaRequestAsync(UnidadMedidaVentaRequest request, string codigo, int? id)
        {
            NormalizeRequired(codigo, "El codigo es obligatorio.");
            NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            if (request.Orden < 0) throw new InvalidOperationException("El orden debe ser mayor o igual a cero.");

            var exists = await _db.UnidadesMedidaVenta.AnyAsync(u => u.Codigo == codigo && (!id.HasValue || u.Id != id.Value));
            if (exists) throw new InvalidOperationException("Ya existe una unidad de medida con ese codigo.");
        }

        private async Task<ValidatedItemFacturableRequest> ValidateItemFacturableRequestAsync(ItemFacturableRequest request, string codigo, int? id)
        {
            NormalizeRequired(codigo, "El codigo es obligatorio.");
            NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            if (request.Orden < 0) throw new InvalidOperationException("El orden debe ser mayor o igual a cero.");
            if (request.PrecioPredeterminado.HasValue && request.PrecioPredeterminado.Value < 0) throw new InvalidOperationException("El precio predeterminado no puede ser negativo.");

            var exists = await _db.ItemsFacturables.AnyAsync(i => i.Codigo == codigo && (!id.HasValue || i.Id != id.Value));
            if (exists) throw new InvalidOperationException("Ya existe un item facturable con ese codigo.");

            CategoriaItemFacturable? categoria = null;
            if (request.CategoriaItemFacturableId.HasValue)
            {
                categoria = await _db.CategoriasItemsFacturables.FirstOrDefaultAsync(c => c.Id == request.CategoriaItemFacturableId.Value);
                if (categoria == null) throw new InvalidOperationException("Categoria de item no encontrada.");
                if (!categoria.Activo) throw new InvalidOperationException("La categoria seleccionada se encuentra inactiva.");
            }

            var unidad = await _db.UnidadesMedidaVenta.FirstOrDefaultAsync(u => u.Id == request.UnidadMedidaVentaId);
            if (unidad == null) throw new InvalidOperationException("Unidad de medida no encontrada.");
            if (!unidad.Activo) throw new InvalidOperationException("La unidad de medida seleccionada se encuentra inactiva.");

            var tratamientoIva = await _db.AlicuotasIvaVenta.FirstOrDefaultAsync(a => a.Id == request.TratamientoIvaPredeterminadoId);
            if (tratamientoIva == null) throw new InvalidOperationException("Tratamiento de IVA no encontrado.");
            if (!tratamientoIva.Activo) throw new InvalidOperationException("El tratamiento de IVA seleccionado se encuentra inactivo.");

            NomencladorFce? nomenclador = null;
            if (request.NomencladorPredeterminadoId.HasValue)
            {
                nomenclador = await _db.NomencladoresFce.FirstOrDefaultAsync(n => n.Id == request.NomencladorPredeterminadoId.Value);
                if (nomenclador == null) throw new InvalidOperationException("Nomenclador FCE no encontrado.");
                if (!nomenclador.Activo) throw new InvalidOperationException("El nomenclador FCE seleccionado se encuentra inactivo.");
            }

            return new ValidatedItemFacturableRequest(categoria, unidad, tratamientoIva, nomenclador);
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

        private static PuntoVentaSelectorResponse MapPuntoVentaSelector(PuntoVentaComprobante relacion)
        {
            var habilitado = relacion.Activo && relacion.PuntoVenta.Activo && relacion.TipoComprobante.Activo;
            return new PuntoVentaSelectorResponse
            {
                PuntoVentaComprobanteId = relacion.Id,
                PuntoVentaId = relacion.PuntoVentaId,
                Numero = relacion.PuntoVenta.Numero,
                Descripcion = relacion.PuntoVenta.Descripcion,
                TextoMostrar = $"{relacion.PuntoVenta.Numero:0000} - {relacion.PuntoVenta.Descripcion}",
                Habilitado = habilitado
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

        private static CategoriaItemFacturableResponse MapCategoriaItemFacturable(CategoriaItemFacturable item)
        {
            return new CategoriaItemFacturableResponse
            {
                Id = item.Id,
                Codigo = item.Codigo,
                Descripcion = item.Descripcion,
                Activo = item.Activo,
                Orden = item.Orden,
                FechaAlta = item.FechaAlta,
                UsuarioAlta = item.UsuarioAlta,
                FechaModificacion = item.FechaModificacion,
                UsuarioModificacion = item.UsuarioModificacion
            };
        }

        private static UnidadMedidaVentaResponse MapUnidadMedida(UnidadMedidaVenta item)
        {
            return new UnidadMedidaVentaResponse
            {
                Id = item.Id,
                Codigo = item.Codigo,
                Descripcion = item.Descripcion,
                Abreviatura = item.Abreviatura,
                PermiteDecimales = item.PermiteDecimales,
                Activo = item.Activo,
                Orden = item.Orden,
                FechaAlta = item.FechaAlta,
                UsuarioAlta = item.UsuarioAlta,
                FechaModificacion = item.FechaModificacion,
                UsuarioModificacion = item.UsuarioModificacion
            };
        }

        private static ItemFacturableResponse MapItemFacturable(ItemFacturable item)
        {
            return new ItemFacturableResponse
            {
                Id = item.Id,
                Codigo = item.Codigo,
                Descripcion = item.Descripcion,
                DescripcionAmpliada = item.DescripcionAmpliada,
                CategoriaItemFacturableId = item.CategoriaItemFacturableId,
                CategoriaCodigo = item.Categoria?.Codigo,
                CategoriaDescripcion = item.Categoria?.Descripcion,
                UnidadMedidaVentaId = item.UnidadMedidaVentaId,
                UnidadMedidaCodigo = item.UnidadMedida.Codigo,
                UnidadMedidaDescripcion = item.UnidadMedida.Descripcion,
                UnidadMedidaAbreviatura = item.UnidadMedida.Abreviatura,
                TratamientoIvaPredeterminadoId = item.TratamientoIvaPredeterminadoId,
                TratamientoIvaCodigo = item.TratamientoIvaPredeterminado.Codigo,
                TratamientoIvaDescripcion = item.TratamientoIvaPredeterminado.Descripcion,
                NomencladorPredeterminadoId = item.NomencladorPredeterminadoId,
                NomencladorCodigo = item.NomencladorPredeterminado?.Codigo,
                NomencladorDescripcion = item.NomencladorPredeterminado?.Descripcion,
                PrecioPredeterminado = item.PrecioPredeterminado,
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
                SubtotalBruto = venta.SubtotalBruto,
                TotalDescuentos = venta.TotalDescuentos,
                NetoGravado = venta.NetoGravado,
                TotalExento = venta.TotalExento,
                TotalNoGravado = venta.TotalNoGravado,
                TotalIva = venta.TotalIva,
                TotalAntesPercepciones = venta.TotalAntesPercepciones,
                TotalPercepciones = venta.TotalPercepciones,
                Total = venta.Total,
                PercepcionIibbRequiereRecalculo = venta.PercepcionIibbRequiereRecalculo,
                FechaUltimoCalculoPercepcion = venta.FechaUltimoCalculoPercepcion,
                Estado = venta.Estado,
                Observaciones = venta.Observaciones,
                FechaAlta = venta.FechaAlta,
                UsuarioAlta = venta.UsuarioAlta,
                FechaModificacion = venta.FechaModificacion,
                UsuarioModificacion = venta.UsuarioModificacion,
                Detalles = venta.Detalles?
                    .OrderBy(d => d.NumeroLinea)
                    .ThenBy(d => d.Id)
                    .GroupBy(d => d.Id)
                    .Select(g => g.First())
                    .Select(MapDetalle)
                    .ToList() ?? new List<VentaDetalleResponse>(),
                PercepcionesIibb = venta.PercepcionesIibb?
                    .Where(p => p.Activa)
                    .OrderBy(p => p.Id)
                    .Select(MapVentaPercepcionIibb)
                    .ToList() ?? new List<VentaPercepcionIibbResponse>()
            };
        }

        private static VentaPercepcionIibbResponse MapVentaPercepcionIibb(VentaPercepcionIibb percepcion)
        {
            return new VentaPercepcionIibbResponse
            {
                Id = percepcion.Id,
                VentaId = percepcion.VentaId,
                RegimenPercepcionIibbId = percepcion.RegimenPercepcionIibbId,
                CodigoRegimenAplicado = percepcion.CodigoRegimenAplicado,
                DescripcionRegimenAplicada = percepcion.DescripcionRegimenAplicada,
                JurisdiccionAplicada = percepcion.JurisdiccionAplicada,
                TipoTributoAplicado = percepcion.TipoTributoAplicado,
                NumeroRegimenAplicado = percepcion.NumeroRegimenAplicado,
                TipoBaseCalculo = percepcion.TipoBaseCalculo,
                BaseImponible = percepcion.BaseImponible,
                AlicuotaAplicada = percepcion.AlicuotaAplicada,
                Importe = percepcion.Importe,
                VigenciaDesdeAplicada = percepcion.VigenciaDesdeAplicada,
                VigenciaHastaAplicada = percepcion.VigenciaHastaAplicada,
                Resultado = percepcion.Resultado,
                Motivo = percepcion.Motivo,
                Activa = percepcion.Activa,
                EsAutomatica = percepcion.EsAutomatica,
                FechaAlta = percepcion.FechaAlta,
                UsuarioAlta = percepcion.UsuarioAlta,
                FechaModificacion = percepcion.FechaModificacion,
                UsuarioModificacion = percepcion.UsuarioModificacion
            };
        }

        private static VentaDetalleResponse MapDetalle(VentaDetalle detalle)
        {
            return new VentaDetalleResponse
            {
                Id = detalle.Id,
                VentaId = detalle.VentaId,
                NumeroLinea = detalle.NumeroLinea,
                ItemFacturableId = detalle.ItemFacturableId,
                CodigoItem = detalle.CodigoItem,
                ItemFacturableDescripcion = detalle.ItemFacturableDescripcion,
                CategoriaItemFacturableId = detalle.CategoriaItemFacturableId,
                CategoriaItemFacturableCodigo = detalle.CategoriaItemFacturableCodigo,
                CategoriaItemFacturableDescripcion = detalle.CategoriaItemFacturableDescripcion,
                UnidadMedidaVentaId = detalle.UnidadMedidaVentaId,
                UnidadMedidaCodigo = detalle.UnidadMedidaCodigo,
                UnidadMedidaDescripcion = detalle.UnidadMedidaDescripcion,
                UnidadMedidaAbreviatura = detalle.UnidadMedidaAbreviatura,
                Descripcion = detalle.Descripcion,
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                PorcentajeDescuento = detalle.PorcentajeDescuento,
                ImporteBruto = detalle.ImporteBruto,
                ImporteDescuento = detalle.ImporteDescuento,
                Neto = detalle.Neto,
                TratamientoIvaId = detalle.TratamientoIvaId,
                TratamientoIvaCodigo = detalle.TratamientoIvaCodigo,
                TratamientoIvaDescripcion = detalle.TratamientoIvaDescripcion,
                TipoTratamientoIva = detalle.TipoTratamientoIva,
                PorcentajeIvaAplicado = detalle.PorcentajeIvaAplicado,
                ImporteIva = detalle.ImporteIva,
                NomencladorId = detalle.NomencladorId,
                NomencladorCodigo = detalle.NomencladorCodigo,
                NomencladorDescripcion = detalle.NomencladorDescripcion,
                TotalLinea = detalle.TotalLinea,
                Observaciones = detalle.Observaciones,
                FechaAlta = detalle.FechaAlta,
                UsuarioAlta = detalle.UsuarioAlta,
                FechaModificacion = detalle.FechaModificacion,
                UsuarioModificacion = detalle.UsuarioModificacion
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
        private sealed record ValidatedDetalleRequest(string Descripcion, ItemFacturable ItemFacturable, AlicuotaIvaVenta TratamientoIva, NomencladorFce? Nomenclador, VentaDetalleCalculo Calculo);
        private sealed record ValidatedItemFacturableRequest(CategoriaItemFacturable? Categoria, UnidadMedidaVenta Unidad, AlicuotaIvaVenta TratamientoIva, NomencladorFce? Nomenclador);
    }
}
