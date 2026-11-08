using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Sales;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Models.Sales;
using BudgetControl.Api.Services.Commercial;
using Microsoft.EntityFrameworkCore;
namespace BudgetControl.Api.Services.Sales;

public class FacturacionHitosService(AppDbContext db)
{
    public async Task<List<FacturaHitoResponse>> GetAsync(int ventaId) => await db.VinculacionesFacturasHitos.AsNoTracking()
        .Where(x => x.VentaId == ventaId).OrderBy(x => x.Orden)
        .Select(x => new FacturaHitoResponse { HitoId = x.HitoId, Importe = x.Importe, ViaId = x.Hito.AcuerdoComercialViaId,
            AcuerdoId = x.Hito.AcuerdoComercialVia.AcuerdoComercialId, NumeroAcuerdo = x.Hito.AcuerdoComercialVia.AcuerdoComercial.NumeroAcuerdo,
            Descripcion = x.Hito.Descripcion, MonedaCodigo = x.Hito.AcuerdoComercialVia.MonedaCodigo }).ToListAsync();

    public async Task ValidarAsync(Venta venta, List<FacturaHitoRequest> items, bool completo)
    {
        if (items.Count == 0) { if (completo) throw new InvalidOperationException("Distribuya el neto de la factura entre los hitos del acuerdo abierto."); return; }
        if (items.Any(x => x.HitoId <= 0 || x.Importe <= 0 || decimal.Round(x.Importe, 2) != x.Importe) || items.Select(x => x.HitoId).Distinct().Count() != items.Count)
            throw new InvalidOperationException("Revise los hitos: no se permiten duplicados ni importes negativos, nulos o con mas de dos decimales.");
        if (await db.VinculacionesFacturaComerciales.AnyAsync(x => x.FacturaExternaId == venta.Id.ToString()))
            throw new InvalidOperationException("Una factura debe imputarse a cuotas o a hitos. Quite primero las imputaciones a cuotas.");
        var ids = items.Select(x => x.HitoId).ToList();
        if (db.Database.CurrentTransaction != null)
        {
            var viaIds = await db.HitosComercialesVias.Where(h => ids.Contains(h.Id)).Select(h => h.AcuerdoComercialViaId).Distinct().OrderBy(id => id).ToArrayAsync();
            await db.AcuerdosComercialesVias.FromSqlInterpolated($"SELECT v.*, v.xmin FROM acuerdos_comerciales_vias v WHERE id = ANY ({viaIds}) ORDER BY id FOR UPDATE").LoadAsync();
            await db.HitosComercialesVias.FromSqlInterpolated($"SELECT * FROM hitos_comerciales_vias WHERE id = ANY ({ids.ToArray()}) ORDER BY id FOR UPDATE").LoadAsync();
        }
        var hitos = await db.HitosComercialesVias.Include(h => h.AcuerdoComercialVia).ThenInclude(v => v.AcuerdoComercial)
            .Where(h => ids.Contains(h.Id)).ToListAsync();
        if (hitos.Count != ids.Count || hitos.Any(h => h.AcuerdoComercialVia.ViaOperacion != ViaOperacion.Via1 ||
            h.AcuerdoComercialVia.ModalidadCobro != ModalidadCobro.Abierta ||
            h.AcuerdoComercialVia.Estado == AcuerdoEstado.Anulado || h.AcuerdoComercialVia.Estado == AcuerdoEstado.Finalizado ||
            h.AcuerdoComercialVia.AcuerdoComercial.Estado == AcuerdoEstado.Anulado || h.AcuerdoComercialVia.AcuerdoComercial.Estado == AcuerdoEstado.Finalizado ||
            h.AcuerdoComercialVia.AcuerdoComercial.ClienteExternoId != venta.ClienteExternoId ||
            h.AcuerdoComercialVia.AcuerdoComercial.ObraExternaId != venta.ObraExternaId))
            throw new InvalidOperationException("Los hitos deben pertenecer a un acuerdo abierto vigente de Via 1 del cliente y obra de la factura.");
        var monedas = hitos.Select(h => h.AcuerdoComercialVia.MonedaCodigo).Distinct().ToList();
        if (monedas.Count != 1) throw new InvalidOperationException("Seleccione hitos de una misma moneda por factura.");
        var neto = ImputacionPlanFactura.Importe(venta, monedas[0]);
        var total = items.Sum(x => x.Importe);
        if (total > neto || (completo && total != neto)) throw new InvalidOperationException($"Distribuya exactamente {neto:N2} {monedas[0]} del neto entre los hitos.");
        var balances = await SaldosHitosVia1.ObtenerAsync(db, ids, venta.Id);
        foreach (var h in hitos)
        {
            var saldo = balances[h.Id];
            if (items.Single(x => x.HitoId == h.Id).Importe > h.ImporteEstimado - saldo.Facturado - saldo.Reservado)
                throw new InvalidOperationException($"El importe supera el saldo disponible para facturar del hito '{h.Descripcion}'.");
        }
        foreach (var via in hitos.Select(h => h.AcuerdoComercialVia).DistinctBy(v => v.Id))
        {
            var comprometido = await db.VinculacionesFacturasHitos.Where(x => x.Hito.AcuerdoComercialViaId == via.Id && x.VentaId != venta.Id &&
                (x.Venta.Estado == VentaEstado.Confirmada || x.Venta.Estado == VentaEstado.Borrador)).SumAsync(x => x.Importe);
            var deVia = items.Where(x => hitos.Any(h => h.Id == x.HitoId && h.AcuerdoComercialViaId == via.Id)).Sum(x => x.Importe);
            if (comprometido + deVia > via.MontoActual) throw new InvalidOperationException("La imputacion supera el monto disponible del acuerdo abierto.");
        }
    }

    public async Task GuardarAsync(Venta venta, List<FacturaHitoRequest> items, string usuario)
    {
        if (venta.Estado != VentaEstado.Borrador) throw new InvalidOperationException("Solo se puede modificar la imputacion de una factura en borrador.");
        await ValidarAsync(venta, items, false);
        db.VinculacionesFacturasHitos.RemoveRange(await db.VinculacionesFacturasHitos.Where(x => x.VentaId == venta.Id).ToListAsync());
        // Delete before adding to allow editing an existing (invoice,hito) unique key.
        await db.SaveChangesAsync();
        var ids = items.Select(x => x.HitoId).ToList();
        var order = await db.HitosComercialesVias.Where(h => ids.Contains(h.Id)).OrderBy(h => h.FechaReferencia).ThenBy(h => h.Id).Select(h => h.Id).ToListAsync();
        foreach (var item in items) db.VinculacionesFacturasHitos.Add(new() { VentaId = venta.Id, HitoId = item.HitoId, Importe = item.Importe,
            Orden = order.IndexOf(item.HitoId), FechaAlta = DateTime.UtcNow, UsuarioAlta = usuario });
        await db.SaveChangesAsync();
    }
}
