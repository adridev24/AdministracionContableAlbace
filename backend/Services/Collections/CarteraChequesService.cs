using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Collections;
using BudgetControl.Api.Models.Collections;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Collections
{
    public class CarteraChequesService : ICarteraChequesService
    {
        private const string CodigoMedioCheque = "CHEQUE";

        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;

        public CarteraChequesService(AppDbContext db, IUserContext userContext)
        {
            _db = db;
            _userContext = userContext;
        }

        public async Task<IEnumerable<ChequeTerceroListResponse>> GetChequesAsync(CarteraChequesFilterRequest filter)
        {
            var query = GetChequeQuery(false);

            if (filter.Estado.HasValue)
            {
                query = query.Where(c => c.Estado == filter.Estado.Value);
            }

            if (filter.FechaVencimientoDesde.HasValue)
            {
                var desde = NormalizeDateOnlyUtc(filter.FechaVencimientoDesde.Value);
                query = query.Where(c => c.FechaVencimiento >= desde);
            }

            if (filter.FechaVencimientoHasta.HasValue)
            {
                var hasta = NormalizeDateOnlyUtc(filter.FechaVencimientoHasta.Value).AddDays(1);
                query = query.Where(c => c.FechaVencimiento < hasta);
            }

            if (!string.IsNullOrWhiteSpace(filter.Moneda))
            {
                var moneda = NormalizeCurrency(filter.Moneda);
                query = query.Where(c => c.MonedaCodigo == moneda);
            }

            if (filter.BancoId.HasValue)
            {
                query = query.Where(c => c.BancoCobranzaId == filter.BancoId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.ClienteId))
            {
                var clienteId = filter.ClienteId.Trim();
                query = query.Where(c => c.CobranzaMedioPago.Cobranza.ClienteExternoId == clienteId);
            }

            var cheques = await query
                .OrderBy(c => c.FechaVencimiento)
                .ThenBy(c => c.Id)
                .ToListAsync();

            return cheques.Select(MapList);
        }

        public async Task<ChequeTerceroDetalleResponse?> GetChequeAsync(int id)
        {
            var cheque = await GetChequeQuery(false).FirstOrDefaultAsync(c => c.Id == id);
            return cheque == null ? null : MapDetalle(cheque);
        }

        public async Task<ChequeTerceroDetalleResponse> DepositarAsync(int id, DepositarChequeTerceroRequest request)
        {
            var cheque = await GetChequeQuery(true).FirstOrDefaultAsync(c => c.Id == id);
            if (cheque == null) throw new InvalidOperationException("Cheque no encontrado.");
            if (cheque.Estado != ChequeTerceroEstado.EN_CARTERA)
            {
                throw new InvalidOperationException("Solo se pueden depositar cheques en cartera.");
            }

            cheque.FechaDeposito = NormalizeRequiredDate(request.FechaDeposito, "La fecha de deposito es obligatoria.");
            cheque.BancoDestino = NormalizeRequired(request.BancoDestino, "El banco destino es obligatorio.");
            cheque.CuentaDestino = NormalizeRequired(request.CuentaDestino, "La cuenta destino es obligatoria.");
            cheque.UsuarioDeposito = _userContext.UserName;
            cheque.Estado = ChequeTerceroEstado.DEPOSITADO;
            cheque.FechaModificacion = DateTime.UtcNow;
            cheque.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            return MapDetalle(cheque);
        }

        public async Task<ChequeTerceroDetalleResponse> AcreditarAsync(int id, AcreditarChequeTerceroRequest request)
        {
            var cheque = await GetChequeQuery(true).FirstOrDefaultAsync(c => c.Id == id);
            if (cheque == null) throw new InvalidOperationException("Cheque no encontrado.");
            if (cheque.Estado != ChequeTerceroEstado.DEPOSITADO)
            {
                throw new InvalidOperationException("Solo se pueden acreditar cheques depositados.");
            }

            cheque.FechaAcreditacion = NormalizeRequiredDate(request.FechaAcreditacion, "La fecha de acreditacion es obligatoria.");
            cheque.UsuarioAcreditacion = _userContext.UserName;
            cheque.Estado = ChequeTerceroEstado.ACREDITADO;
            cheque.FechaModificacion = DateTime.UtcNow;
            cheque.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            return MapDetalle(cheque);
        }

        public async Task EnsureChequesDesdeCobranzaConfirmadaAsync(Cobranza cobranza)
        {
            var mediosCheque = cobranza.MediosPago
                .Where(m => string.Equals(m.MedioPago.Codigo, CodigoMedioCheque, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!mediosCheque.Any()) return;

            var medioIds = mediosCheque.Select(m => m.Id).ToList();
            var existentes = await _db.ChequesTerceros
                .Where(c => medioIds.Contains(c.CobranzaMedioPagoId))
                .Select(c => c.CobranzaMedioPagoId)
                .ToListAsync();
            var existentesSet = existentes.ToHashSet();

            foreach (var medio in mediosCheque)
            {
                if (existentesSet.Contains(medio.Id)) continue;
                ValidateMedioChequeParaCartera(medio);

                _db.ChequesTerceros.Add(new ChequeTercero
                {
                    CobranzaMedioPagoId = medio.Id,
                    BancoCobranzaId = medio.BancoCobranzaId!.Value,
                    NumeroCheque = medio.NumeroReferencia!.Trim(),
                    FechaEmision = NormalizeRequiredDate(medio.FechaEmision!.Value, "La fecha de emision del cheque es obligatoria."),
                    FechaVencimiento = NormalizeRequiredDate(medio.FechaValor!.Value, "La fecha de vencimiento del cheque es obligatoria."),
                    Importe = RoundMoney(medio.Importe),
                    MonedaCodigo = NormalizeCurrency(cobranza.MonedaCodigo),
                    Librador = medio.Librador!.Trim(),
                    CuitLibrador = medio.CuitLibrador!.Trim(),
                    Estado = ChequeTerceroEstado.EN_CARTERA,
                    Observaciones = medio.Observaciones,
                    FechaAlta = DateTime.UtcNow,
                    UsuarioAlta = _userContext.UserName
                });
            }
        }

        private IQueryable<ChequeTercero> GetChequeQuery(bool tracking)
        {
            var query = _db.ChequesTerceros
                .Include(c => c.BancoCatalogo)
                .Include(c => c.CobranzaMedioPago)
                    .ThenInclude(m => m.MedioPago)
                .Include(c => c.CobranzaMedioPago)
                    .ThenInclude(m => m.Cobranza)
                .AsQueryable();

            return tracking ? query : query.AsNoTracking();
        }

        private static void ValidateMedioChequeParaCartera(CobranzaMedioPago medio)
        {
            if (!medio.BancoCobranzaId.HasValue) throw new InvalidOperationException("El cheque requiere banco.");
            if (string.IsNullOrWhiteSpace(medio.NumeroReferencia)) throw new InvalidOperationException("El cheque requiere numero.");
            if (!medio.FechaEmision.HasValue) throw new InvalidOperationException("El cheque requiere fecha de emision.");
            if (!medio.FechaValor.HasValue) throw new InvalidOperationException("El cheque requiere fecha de vencimiento.");
            if (medio.Importe <= 0) throw new InvalidOperationException("El importe del cheque debe ser mayor a cero.");
            if (string.IsNullOrWhiteSpace(medio.Librador)) throw new InvalidOperationException("El cheque requiere librador.");
            if (string.IsNullOrWhiteSpace(medio.CuitLibrador)) throw new InvalidOperationException("El cheque requiere CUIT del librador.");
        }

        private static ChequeTerceroListResponse MapList(ChequeTercero cheque)
        {
            return new ChequeTerceroListResponse
            {
                Id = cheque.Id,
                NumeroCheque = cheque.NumeroCheque,
                BancoCobranzaId = cheque.BancoCobranzaId,
                Banco = cheque.BancoCatalogo.Nombre,
                FechaVencimiento = cheque.FechaVencimiento,
                Importe = cheque.Importe,
                MonedaCodigo = cheque.MonedaCodigo,
                Librador = cheque.Librador,
                Estado = cheque.Estado,
                ClienteExternoId = cheque.CobranzaMedioPago.Cobranza.ClienteExternoId,
                CobranzaId = cheque.CobranzaMedioPago.CobranzaId
            };
        }

        private static ChequeTerceroDetalleResponse MapDetalle(ChequeTercero cheque)
        {
            var list = MapList(cheque);
            return new ChequeTerceroDetalleResponse
            {
                Id = list.Id,
                NumeroCheque = list.NumeroCheque,
                BancoCobranzaId = list.BancoCobranzaId,
                Banco = list.Banco,
                FechaVencimiento = list.FechaVencimiento,
                Importe = list.Importe,
                MonedaCodigo = list.MonedaCodigo,
                Librador = list.Librador,
                Estado = list.Estado,
                ClienteExternoId = list.ClienteExternoId,
                CobranzaId = list.CobranzaId,
                CobranzaMedioPagoId = cheque.CobranzaMedioPagoId,
                FechaEmision = cheque.FechaEmision,
                CuitLibrador = cheque.CuitLibrador,
                Observaciones = cheque.Observaciones,
                FechaAlta = cheque.FechaAlta,
                UsuarioAlta = cheque.UsuarioAlta,
                FechaModificacion = cheque.FechaModificacion,
                UsuarioModificacion = cheque.UsuarioModificacion,
                FechaDeposito = cheque.FechaDeposito,
                BancoDestino = cheque.BancoDestino,
                CuentaDestino = cheque.CuentaDestino,
                UsuarioDeposito = cheque.UsuarioDeposito,
                FechaAcreditacion = cheque.FechaAcreditacion,
                UsuarioAcreditacion = cheque.UsuarioAcreditacion,
                MedioPagoCodigo = cheque.CobranzaMedioPago.MedioPago.Codigo,
                MedioPagoDescripcion = cheque.CobranzaMedioPago.MedioPago.Descripcion
            };
        }

        private static DateTime NormalizeRequiredDate(DateTime value, string errorMessage)
        {
            if (value == default) throw new InvalidOperationException(errorMessage);
            return NormalizeDateOnlyUtc(value);
        }

        private static DateTime NormalizeDateOnlyUtc(DateTime value)
        {
            return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
        }

        private static string NormalizeRequired(string? value, string errorMessage)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized)) throw new InvalidOperationException(errorMessage);
            return normalized;
        }

        private static string NormalizeCurrency(string? value)
        {
            var normalized = NormalizeRequired(value, "La moneda es obligatoria.").ToUpperInvariant();
            if (normalized.Length > 10) throw new InvalidOperationException("La moneda no es valida.");
            return normalized;
        }

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
