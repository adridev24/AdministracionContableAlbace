using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Accounting;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Accounting
{
    public class ContabilizacionAutomaticaService : IContabilizacionAutomaticaService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguracionesContablesService _configuracionesService;
        private readonly IAsientosContablesService _asientosService;

        public ContabilizacionAutomaticaService(
            AppDbContext db,
            IConfiguracionesContablesService configuracionesService,
            IAsientosContablesService asientosService)
        {
            _db = db;
            _configuracionesService = configuracionesService;
            _asientosService = asientosService;
        }

        public async Task<ContabilizacionAutomaticaResponse> GenerarAsientoAutomaticoAsync(SolicitudContabilizacionAutomaticaRequest request)
        {
            var codigoOperacion = NormalizeRequiredUpper(request.CodigoOperacion, "El codigo de operacion es obligatorio.");
            var moduloOrigen = NormalizeRequired(request.ModuloOrigen, "El modulo origen es obligatorio.");
            var idOrigen = NormalizeRequired(request.IdOrigen, "El identificador origen es obligatorio.");
            var descripcion = NormalizeRequired(request.Descripcion, "La descripcion es obligatoria.");
            var idOrigenContable = BuildIdOrigenContable(codigoOperacion, idOrigen);

            var existing = await _db.AsientosContables
                .AsNoTracking()
                .Where(a => a.EsAutomatico && !a.EsReversion && a.ModuloOrigen == moduloOrigen && a.IdOrigen == idOrigenContable)
                .OrderBy(a => a.Id)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return new ContabilizacionAutomaticaResponse
                {
                    AsientoContableId = existing.Id,
                    YaExistia = true,
                    CodigoOperacion = codigoOperacion,
                    ModuloOrigen = moduloOrigen,
                    IdOrigen = idOrigen,
                    IdOrigenContable = idOrigenContable
                };
            }

            var configuracion = await _configuracionesService.GetConfiguracionPorOperacionAsync(codigoOperacion);
            if (configuracion == null)
            {
                throw new InvalidOperationException("No existe una configuracion contable activa para la operacion indicada.");
            }

            if (!configuracion.Activa)
            {
                throw new InvalidOperationException("La configuracion contable indicada se encuentra inactiva.");
            }

            var importes = NormalizeImportes(request.ImportesPorConcepto);
            ValidateConceptos(configuracion, importes);

            var detalles = configuracion.Detalles
                .OrderBy(d => d.Orden)
                .Select(detalle =>
                {
                    var importe = importes[detalle.Concepto];
                    return new CrearAsientoContableDetalleRequest
                    {
                        CuentaContableId = detalle.CuentaContableId,
                        Descripcion = $"{detalle.Concepto} - {detalle.CuentaNombre}",
                        Debe = detalle.TipoMovimiento == "Debe" ? importe : 0,
                        Haber = detalle.TipoMovimiento == "Haber" ? importe : 0
                    };
                })
                .ToList();

            var asiento = await _asientosService.GenerarAsientoAutomaticoAsync(
                moduloOrigen,
                idOrigenContable,
                request.Fecha,
                descripcion,
                detalles);

            return new ContabilizacionAutomaticaResponse
            {
                AsientoContableId = asiento.Id,
                YaExistia = false,
                CodigoOperacion = codigoOperacion,
                ModuloOrigen = moduloOrigen,
                IdOrigen = idOrigen,
                IdOrigenContable = idOrigenContable
            };
        }

        private static Dictionary<string, decimal> NormalizeImportes(Dictionary<string, decimal>? importes)
        {
            if (importes == null || importes.Count == 0)
            {
                throw new InvalidOperationException("Debe informar importes por concepto.");
            }

            var normalized = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in importes)
            {
                var concepto = NormalizeRequiredUpper(item.Key, "El concepto es obligatorio.");
                var importe = Math.Round(item.Value, 2, MidpointRounding.AwayFromZero);
                if (importe < 0)
                {
                    throw new InvalidOperationException("Los importes por concepto no pueden ser negativos.");
                }

                if (normalized.ContainsKey(concepto))
                {
                    throw new InvalidOperationException("No se permiten conceptos duplicados en la solicitud.");
                }

                normalized.Add(concepto, importe);
            }

            return normalized;
        }

        private static void ValidateConceptos(ConfiguracionContableResponse configuracion, IReadOnlyDictionary<string, decimal> importes)
        {
            var conceptosConfigurados = configuracion.Detalles
                .Select(d => d.Concepto)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var desconocido = importes.Keys.FirstOrDefault(concepto => !conceptosConfigurados.Contains(concepto));
            if (!string.IsNullOrWhiteSpace(desconocido))
            {
                throw new InvalidOperationException($"El concepto {desconocido} no esta definido en la configuracion contable.");
            }

            foreach (var detalle in configuracion.Detalles)
            {
                if (!importes.TryGetValue(detalle.Concepto, out var importe) || importe <= 0)
                {
                    throw new InvalidOperationException($"Debe informar un importe mayor a cero para el concepto {detalle.Concepto}.");
                }
            }

            if (!configuracion.Detalles.Any(d => d.TipoMovimiento == "Debe"))
            {
                throw new InvalidOperationException("La configuracion contable debe tener al menos un movimiento Debe.");
            }

            if (!configuracion.Detalles.Any(d => d.TipoMovimiento == "Haber"))
            {
                throw new InvalidOperationException("La configuracion contable debe tener al menos un movimiento Haber.");
            }
        }

        private static string BuildIdOrigenContable(string codigoOperacion, string idOrigen)
        {
            return $"{codigoOperacion}|{idOrigen}";
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

        private static string NormalizeRequiredUpper(string? value, string errorMessage)
        {
            return NormalizeRequired(value, errorMessage).ToUpperInvariant();
        }
    }
}
