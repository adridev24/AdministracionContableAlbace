using BudgetControl.Api.Data;
using BudgetControl.Api.Models.Collections;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Collections;

public static class SaldosCuotasCobranza
{
    public static async Task<Dictionary<int, decimal>> ObtenerAsync(AppDbContext db, IEnumerable<int> ids, int? ventaId = null, int? excluirCobranzaId = null, DateTime? desde = null, DateTime? hasta = null)
    {
        var cuotas = ids.ToHashSet();
        var aplicaciones = await db.CobranzasAplicacionesFactura.AsNoTracking()
            .Include(a => a.AplicacionesObligacion)
            .Include(a => a.AplicacionesMediosPago).ThenInclude(d => d.CobranzaMedioPago).ThenInclude(m => m.ChequeTercero)
            .Where(a => a.Cobranza.Estado == CobranzaEstado.Confirmada &&
                (!ventaId.HasValue || a.VentaId == ventaId) &&
                (!desde.HasValue || a.Cobranza.Fecha >= desde) && (!hasta.HasValue || a.Cobranza.Fecha <= hasta) &&
                (!excluirCobranzaId.HasValue || a.CobranzaId != excluirCobranzaId) &&
                a.AplicacionesObligacion.Any(o => cuotas.Contains(o.CuotaComercialId)))
            .ToListAsync();
        var result = new Dictionary<int, decimal>();
        foreach (var aplicacion in aplicaciones)
        {
            var rechazado = aplicacion.AplicacionesMediosPago
                .Where(d => d.CobranzaMedioPago.ChequeTercero?.Estado == ChequeTerceroEstado.RECHAZADO)
                .Sum(d => d.ImporteAplicado);
            var totalPlan = aplicacion.AplicacionesObligacion.Sum(o => o.ImporteAplicado);
            var restanteRechazado = aplicacion.ImporteAplicado > 0
                ? decimal.Round(totalPlan * Math.Min(rechazado / aplicacion.ImporteAplicado, 1), 2, MidpointRounding.AwayFromZero) : 0;
            foreach (var obligacion in aplicacion.AplicacionesObligacion.OrderBy(o => o.Id))
            {
                var descuento = Math.Min(restanteRechazado, obligacion.ImporteAplicado);
                restanteRechazado -= descuento;
                if (cuotas.Contains(obligacion.CuotaComercialId))
                    result[obligacion.CuotaComercialId] = result.GetValueOrDefault(obligacion.CuotaComercialId) + obligacion.ImporteAplicado - descuento;
            }
        }
        return result;
    }
}
