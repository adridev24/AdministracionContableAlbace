using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Sales;
using BudgetControl.Api.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Sales
{
    public class PercepcionIibbService : IPercepcionIibbService
    {
        private readonly AppDbContext _db;
        private readonly IExternalDataService _externalDataService;
        private readonly IUserContext _userContext;
        private readonly ICalculadorVentasService _calculador;

        public PercepcionIibbService(AppDbContext db, IExternalDataService externalDataService, IUserContext userContext, ICalculadorVentasService calculador)
        {
            _db = db;
            _externalDataService = externalDataService;
            _userContext = userContext;
            _calculador = calculador;
        }

        public async Task<ClientePercepcionIibbConfigResponse?> GetClienteConfigAsync(string clienteExternoId)
        {
            var config = await GetClienteConfigQuery()
                .FirstOrDefaultAsync(c => c.ClienteExternoId == clienteExternoId);

            return config == null ? null : MapClienteConfig(config);
        }

        public async Task<ClientePercepcionIibbConfigResponse> SaveClienteConfigAsync(ClientePercepcionIibbConfigRequest request)
        {
            var clienteId = NormalizeRequired(request.ClienteExternoId, "El cliente es obligatorio.");
            if (!int.TryParse(clienteId, out var parsedClienteId)) throw new InvalidOperationException("Cliente invalido.");
            var cliente = await _externalDataService.GetClientByIdAsync(parsedClienteId);
            if (cliente == null) throw new InvalidOperationException("Cliente no encontrado.");

            PercepcionIibbEntreRios? regimen = null;
            if (request.Situacion == SituacionPercepcionIibbCliente.Alcanzado)
            {
                if (!request.RegimenPercepcionIibbId.HasValue) throw new InvalidOperationException("Debe indicar un regimen para un cliente alcanzado.");
                regimen = await _db.PercepcionesIibbEntreRios.FirstOrDefaultAsync(r => r.Id == request.RegimenPercepcionIibbId.Value);
                if (regimen == null) throw new InvalidOperationException("Regimen de percepcion no encontrado.");
                if (!regimen.Activo) throw new InvalidOperationException("El regimen de percepcion se encuentra inactivo.");
            }

            if (request.Situacion == SituacionPercepcionIibbCliente.Excluido)
            {
                if (!request.ExclusionDesde.HasValue || !request.ExclusionHasta.HasValue)
                {
                    throw new InvalidOperationException("La exclusion requiere vigencia desde y hasta.");
                }
                if (request.ExclusionHasta.Value.Date < request.ExclusionDesde.Value.Date)
                {
                    throw new InvalidOperationException("La vigencia hasta de la exclusion no puede ser anterior a la vigencia desde.");
                }
            }

            var config = await _db.ClientesPercepcionIibbConfig
                .Include(c => c.RegimenPercepcionIibb)
                .FirstOrDefaultAsync(c => c.ClienteExternoId == clienteId);

            if (config == null)
            {
                config = new ClientePercepcionIibbConfig
                {
                    ClienteExternoId = clienteId,
                    FechaAlta = DateTime.UtcNow,
                    UsuarioAlta = _userContext.UserName
                };
                _db.ClientesPercepcionIibbConfig.Add(config);
            }
            else
            {
                config.FechaModificacion = DateTime.UtcNow;
                config.UsuarioModificacion = _userContext.UserName;
            }

            config.Situacion = request.Situacion;
            config.RegimenPercepcionIibbId = request.Situacion == SituacionPercepcionIibbCliente.Alcanzado ? request.RegimenPercepcionIibbId : null;
            config.RegimenPercepcionIibb = regimen;
            config.NumeroInscripcionIibb = NormalizeOptional(request.NumeroInscripcionIibb);
            config.JurisdiccionIibb = NormalizeOptional(request.JurisdiccionIibb);
            config.ExclusionDesde = request.ExclusionDesde.HasValue ? EnsureUtcDate(request.ExclusionDesde.Value) : null;
            config.ExclusionHasta = request.ExclusionHasta.HasValue ? EnsureUtcDate(request.ExclusionHasta.Value) : null;
            config.MotivoExclusion = NormalizeOptional(request.MotivoExclusion);
            config.Observaciones = NormalizeOptional(request.Observaciones);

            await _db.SaveChangesAsync();
            return MapClienteConfig(config);
        }

        public async Task<VentaPercepcionIibbResponse?> GetPercepcionAsync(int ventaId)
        {
            var percepcion = await _db.VentasPercepcionesIibb
                .AsNoTracking()
                .Where(p => p.VentaId == ventaId && p.Activa)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            return percepcion == null ? null : MapPercepcion(percepcion);
        }

        public async Task<VentaPercepcionIibbCalculoResponse> CalcularAsync(int ventaId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var venta = await GetVentaForPercepcionAsync(ventaId);
            if (venta.Estado != VentaEstado.Borrador) throw new InvalidOperationException("Solo una venta en estado Borrador permite recalcular percepciones.");

            var config = await GetClienteConfigQuery().FirstOrDefaultAsync(c => c.ClienteExternoId == venta.ClienteExternoId);
            _calculador.RecalcularTotales(venta);
            var result = await DeterminarAsync(venta, config);
            var percepcion = await UpsertPercepcionAsync(venta, result);

            venta.PercepcionIibbRequiereRecalculo = false;
            venta.FechaUltimoCalculoPercepcion = DateTime.UtcNow;
            venta.FechaModificacion = DateTime.UtcNow;
            venta.UsuarioModificacion = _userContext.UserName;
            _calculador.RecalcularTotales(venta);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new VentaPercepcionIibbCalculoResponse
            {
                Percepcion = MapPercepcion(percepcion),
                ConfiguracionCliente = config == null ? null : MapClienteConfig(config)
            };
        }

        private async Task<PercepcionDetermination> DeterminarAsync(Venta venta, ClientePercepcionIibbConfig? config)
        {
            if (venta.TipoComprobante.EsExportacion)
            {
                return PercepcionDetermination.NoAplica(ResultadoPercepcionIibb.Exportacion, "El comprobante de exportacion no aplica percepcion de IIBB Entre Rios.");
            }

            if (config == null || config.Situacion == SituacionPercepcionIibbCliente.Pendiente)
            {
                return PercepcionDetermination.NoAplica(ResultadoPercepcionIibb.ClienteSinConfigurar, "El cliente no tiene configurada su situacion frente a percepcion de IIBB Entre Rios.");
            }

            if (config.Situacion == SituacionPercepcionIibbCliente.NoAlcanzado)
            {
                return PercepcionDetermination.NoAplica(ResultadoPercepcionIibb.NoCorresponde, "El cliente esta configurado como no alcanzado por percepcion de IIBB Entre Rios.");
            }

            if (config.Situacion == SituacionPercepcionIibbCliente.Excluido)
            {
                if (!config.ExclusionDesde.HasValue || !config.ExclusionHasta.HasValue)
                {
                    return PercepcionDetermination.NoAplica(ResultadoPercepcionIibb.ClienteSinConfigurar, "La exclusion del cliente no tiene vigencia completa configurada.");
                }

                var fecha = venta.FechaComprobante.Date;
                if (config.ExclusionDesde.Value.Date <= fecha && fecha <= config.ExclusionHasta.Value.Date)
                {
                    return PercepcionDetermination.NoAplica(ResultadoPercepcionIibb.Excluido, "El cliente posee exclusion vigente de percepcion de IIBB Entre Rios.");
                }
            }

            if (!config.RegimenPercepcionIibbId.HasValue)
            {
                return PercepcionDetermination.NoAplica(ResultadoPercepcionIibb.SinRegimen, "El cliente no tiene regimen de percepcion asignado.");
            }

            var regimen = await _db.PercepcionesIibbEntreRios.FirstOrDefaultAsync(r => r.Id == config.RegimenPercepcionIibbId.Value);
            if (regimen == null || !regimen.Activo)
            {
                return PercepcionDetermination.NoAplica(ResultadoPercepcionIibb.SinRegimen, "El regimen de percepcion asignado no existe o se encuentra inactivo.");
            }

            if (!IsEntreRios(regimen.Jurisdiccion) || !regimen.TipoTributo.Equals("PERCEPCION_IIBB", StringComparison.OrdinalIgnoreCase))
            {
                return PercepcionDetermination.NoAplica(ResultadoPercepcionIibb.SinRegimen, "El regimen asignado no corresponde a percepcion de IIBB Entre Rios.");
            }

            var fechaComprobante = venta.FechaComprobante.Date;
            if (regimen.VigenciaDesde.Date > fechaComprobante || (regimen.VigenciaHasta.HasValue && regimen.VigenciaHasta.Value.Date < fechaComprobante))
            {
                return PercepcionDetermination.NoAplica(ResultadoPercepcionIibb.RegimenVencido, "El regimen no se encuentra vigente para la fecha de la factura.", regimen);
            }

            var baseImponible = CalcularBase(venta, regimen.TipoBaseCalculo);
            if (!baseImponible.HasValue)
            {
                return PercepcionDetermination.NoAplica(ResultadoPercepcionIibb.BaseNoSoportada, "El tipo de base de calculo del regimen requiere configuracion adicional no implementada en esta etapa.", regimen);
            }

            if (regimen.MontoMinimo.HasValue && baseImponible.Value < regimen.MontoMinimo.Value)
            {
                return PercepcionDetermination.NoAplica(ResultadoPercepcionIibb.BaseInferiorMinimo, "La base imponible no supera el monto minimo configurado para el regimen.", regimen, baseImponible.Value);
            }

            var importe = RoundMoney(baseImponible.Value * regimen.Porcentaje / 100m);
            if (importe <= 0)
            {
                return PercepcionDetermination.NoAplica(ResultadoPercepcionIibb.NoCorresponde, "El calculo de percepcion no arroja importe a aplicar.", regimen, baseImponible.Value);
            }

            return new PercepcionDetermination(ResultadoPercepcionIibb.Aplicada, "Percepcion de IIBB Entre Rios aplicada.", regimen, baseImponible.Value, regimen.Porcentaje, importe);
        }

        private async Task<VentaPercepcionIibb> UpsertPercepcionAsync(Venta venta, PercepcionDetermination result)
        {
            var percepcion = venta.PercepcionesIibb.FirstOrDefault(p => p.Activa);
            if (percepcion == null)
            {
                percepcion = new VentaPercepcionIibb
                {
                    VentaId = venta.Id,
                    Activa = true,
                    EsAutomatica = true,
                    FechaAlta = DateTime.UtcNow,
                    UsuarioAlta = _userContext.UserName
                };
                _db.VentasPercepcionesIibb.Add(percepcion);
                venta.PercepcionesIibb.Add(percepcion);
            }
            else
            {
                percepcion.FechaModificacion = DateTime.UtcNow;
                percepcion.UsuarioModificacion = _userContext.UserName;
            }

            var regimen = result.Regimen;
            percepcion.RegimenPercepcionIibbId = regimen?.Id;
            percepcion.RegimenPercepcionIibb = regimen;
            percepcion.CodigoRegimenAplicado = regimen?.Codigo;
            percepcion.DescripcionRegimenAplicada = regimen?.Descripcion;
            percepcion.JurisdiccionAplicada = regimen?.Jurisdiccion;
            percepcion.TipoTributoAplicado = regimen?.TipoTributo;
            percepcion.NumeroRegimenAplicado = regimen?.NumeroRegimen;
            percepcion.TipoBaseCalculo = regimen?.TipoBaseCalculo;
            percepcion.BaseImponible = result.BaseImponible;
            percepcion.AlicuotaAplicada = result.Alicuota;
            percepcion.Importe = result.Importe;
            percepcion.VigenciaDesdeAplicada = regimen?.VigenciaDesde;
            percepcion.VigenciaHastaAplicada = regimen?.VigenciaHasta;
            percepcion.Resultado = result.Resultado;
            percepcion.Motivo = result.Motivo;
            percepcion.Activa = true;
            percepcion.EsAutomatica = true;

            await Task.CompletedTask;
            return percepcion;
        }

        private IQueryable<ClientePercepcionIibbConfig> GetClienteConfigQuery()
        {
            return _db.ClientesPercepcionIibbConfig
                .AsNoTracking()
                .Include(c => c.RegimenPercepcionIibb);
        }

        private async Task<Venta> GetVentaForPercepcionAsync(int ventaId)
        {
            var venta = await _db.Ventas
                .Include(v => v.TipoComprobante)
                .Include(v => v.Detalles)
                .Include(v => v.PercepcionesIibb)
                .FirstOrDefaultAsync(v => v.Id == ventaId);

            return venta ?? throw new InvalidOperationException("Venta no encontrada.");
        }

        private static decimal? CalcularBase(Venta venta, TipoBaseCalculoPercepcionIibb tipoBase)
        {
            return tipoBase switch
            {
                TipoBaseCalculoPercepcionIibb.NetoGravado => venta.NetoGravado,
                TipoBaseCalculoPercepcionIibb.NetoTotal => venta.NetoGravado + venta.TotalExento + venta.TotalNoGravado,
                TipoBaseCalculoPercepcionIibb.TotalSinIva => venta.NetoGravado + venta.TotalExento + venta.TotalNoGravado,
                TipoBaseCalculoPercepcionIibb.OtraBaseConfigurable => null,
                _ => null
            };
        }

        private static ClientePercepcionIibbConfigResponse MapClienteConfig(ClientePercepcionIibbConfig config)
        {
            return new ClientePercepcionIibbConfigResponse
            {
                Id = config.Id,
                ClienteExternoId = config.ClienteExternoId,
                Situacion = config.Situacion,
                RegimenPercepcionIibbId = config.RegimenPercepcionIibbId,
                RegimenCodigo = config.RegimenPercepcionIibb?.Codigo,
                RegimenDescripcion = config.RegimenPercepcionIibb?.Descripcion,
                NumeroInscripcionIibb = config.NumeroInscripcionIibb,
                JurisdiccionIibb = config.JurisdiccionIibb,
                ExclusionDesde = config.ExclusionDesde,
                ExclusionHasta = config.ExclusionHasta,
                MotivoExclusion = config.MotivoExclusion,
                Observaciones = config.Observaciones,
                FechaAlta = config.FechaAlta,
                UsuarioAlta = config.UsuarioAlta,
                FechaModificacion = config.FechaModificacion,
                UsuarioModificacion = config.UsuarioModificacion
            };
        }

        private static VentaPercepcionIibbResponse MapPercepcion(VentaPercepcionIibb percepcion)
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

        private static bool IsEntreRios(string value)
        {
            var normalized = value.Trim().ToUpperInvariant().Replace("Í", "I");
            return normalized == "ENTRE RIOS";
        }

        private static DateTime EnsureUtcDate(DateTime value)
        {
            var date = value.Date;
            return date.Kind == DateTimeKind.Utc ? date : DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }

        private static string NormalizeRequired(string? value, string message)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(message);
            return value.Trim();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private sealed record PercepcionDetermination(
            ResultadoPercepcionIibb Resultado,
            string Motivo,
            PercepcionIibbEntreRios? Regimen,
            decimal BaseImponible,
            decimal Alicuota,
            decimal Importe)
        {
            public static PercepcionDetermination NoAplica(ResultadoPercepcionIibb resultado, string motivo, PercepcionIibbEntreRios? regimen = null, decimal baseImponible = 0)
            {
                return new PercepcionDetermination(resultado, motivo, regimen, baseImponible, 0, 0);
            }
        }
    }
}
