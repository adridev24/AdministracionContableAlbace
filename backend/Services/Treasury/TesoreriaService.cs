using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Treasury;
using BudgetControl.Api.Models.Collections;
using BudgetControl.Api.Models.Treasury;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Treasury
{
    public class TesoreriaService : ITesoreriaService
    {
        private static readonly HashSet<string> TiposCuentaValidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "CUENTA_CORRIENTE",
            "CAJA_AHORRO",
            "OTRA"
        };

        private static readonly HashSet<string> MonedasValidas = new(StringComparer.OrdinalIgnoreCase)
        {
            "ARS",
            "USD"
        };

        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;

        public TesoreriaService(AppDbContext db, IUserContext userContext)
        {
            _db = db;
            _userContext = userContext;
        }

        public async Task<IEnumerable<BancoResponse>> GetBancosAsync(BancoFilterRequest filter)
        {
            var query = _db.BancosCobranza.AsNoTracking().AsQueryable();

            if (filter.Activo.HasValue)
            {
                query = query.Where(b => b.Activo == filter.Activo.Value);
            }

            var search = filter.Search?.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b => b.Codigo.ToLower().Contains(search) || b.Nombre.ToLower().Contains(search));
            }

            var bancos = await query
                .OrderBy(b => b.Orden)
                .ThenBy(b => b.Nombre)
                .ToListAsync();

            return bancos.Select(MapBanco);
        }

        public async Task<BancoResponse?> GetBancoAsync(int id)
        {
            var banco = await _db.BancosCobranza.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            return banco == null ? null : MapBanco(banco);
        }

        public async Task<BancoResponse> CreateBancoAsync(UpsertBancoRequest request)
        {
            var codigo = NormalizeCodigo(request.Codigo);
            var nombre = NormalizeRequired(request.Nombre, "El nombre del banco es obligatorio.");
            await EnsureCodigoBancoDisponibleAsync(codigo);

            var nextOrden = await _db.BancosCobranza.AnyAsync()
                ? await _db.BancosCobranza.MaxAsync(b => b.Orden) + 10
                : 10;

            var banco = new BancoCobranza
            {
                Codigo = codigo,
                Nombre = nombre,
                Activo = request.Activo,
                Orden = nextOrden,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            _db.BancosCobranza.Add(banco);
            await _db.SaveChangesAsync();
            if (!request.Activo)
            {
                banco.Activo = false;
                await _db.SaveChangesAsync();
            }

            return MapBanco(banco);
        }

        public async Task<BancoResponse> UpdateBancoAsync(int id, UpsertBancoRequest request)
        {
            var banco = await _db.BancosCobranza.FirstOrDefaultAsync(b => b.Id == id);
            if (banco == null)
            {
                throw new KeyNotFoundException("Banco no encontrado.");
            }

            var codigo = NormalizeCodigo(request.Codigo);
            var nombre = NormalizeRequired(request.Nombre, "El nombre del banco es obligatorio.");
            if (!string.Equals(banco.Codigo, codigo, StringComparison.OrdinalIgnoreCase))
            {
                await EnsureCodigoBancoDisponibleAsync(codigo, id);
                banco.Codigo = codigo;
            }

            banco.Nombre = nombre;
            banco.Activo = request.Activo;
            banco.FechaModificacion = DateTime.UtcNow;
            banco.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            return MapBanco(banco);
        }

        public async Task<IEnumerable<CuentaBancariaEmpresaResponse>> GetCuentasBancariasAsync(CuentaBancariaEmpresaFilterRequest filter)
        {
            var query = GetCuentaQuery(false);

            if (filter.BancoId.HasValue)
            {
                query = query.Where(c => c.BancoId == filter.BancoId.Value);
            }

            var moneda = NormalizeCurrencyFilter(filter.MonedaCodigo);
            if (!string.IsNullOrWhiteSpace(moneda))
            {
                query = query.Where(c => c.MonedaCodigo == moneda);
            }

            if (filter.Activa.HasValue)
            {
                query = query.Where(c => c.Activa == filter.Activa.Value);
            }

            var search = filter.Search?.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Descripcion.ToLower().Contains(search) ||
                    c.NumeroCuenta.ToLower().Contains(search) ||
                    c.Banco.Nombre.ToLower().Contains(search) ||
                    (c.AliasCbu != null && c.AliasCbu.ToLower().Contains(search)));
            }

            var cuentas = await query
                .OrderBy(c => c.Banco.Nombre)
                .ThenBy(c => c.Descripcion)
                .ToListAsync();

            return cuentas.Select(MapCuenta);
        }

        public async Task<CuentaBancariaEmpresaResponse?> GetCuentaBancariaAsync(int id)
        {
            var cuenta = await GetCuentaQuery(false).FirstOrDefaultAsync(c => c.Id == id);
            return cuenta == null ? null : MapCuenta(cuenta);
        }

        public async Task<CuentaBancariaEmpresaResponse> CreateCuentaBancariaAsync(UpsertCuentaBancariaEmpresaRequest request)
        {
            var data = await ValidateCuentaRequestAsync(request);
            await EnsureCuentaBancoNumeroDisponibleAsync(data.Banco.Id, data.NumeroCuenta, data.MonedaCodigo);

            var cuenta = new CuentaBancariaEmpresa
            {
                BancoId = data.Banco.Id,
                Descripcion = data.Descripcion,
                TipoCuenta = data.TipoCuenta,
                MonedaCodigo = data.MonedaCodigo,
                NumeroCuenta = data.NumeroCuenta,
                Cbu = data.Cbu,
                AliasCbu = data.AliasCbu,
                CuentaContableId = data.CuentaContableId,
                Activa = request.Activa,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            _db.CuentasBancariasEmpresa.Add(cuenta);
            await _db.SaveChangesAsync();
            if (!request.Activa)
            {
                cuenta.Activa = false;
                await _db.SaveChangesAsync();
            }

            return (await GetCuentaBancariaAsync(cuenta.Id))!;
        }

        public async Task<CuentaBancariaEmpresaResponse> UpdateCuentaBancariaAsync(int id, UpsertCuentaBancariaEmpresaRequest request)
        {
            var cuenta = await _db.CuentasBancariasEmpresa.FirstOrDefaultAsync(c => c.Id == id);
            if (cuenta == null)
            {
                throw new KeyNotFoundException("Cuenta bancaria no encontrada.");
            }

            var data = await ValidateCuentaRequestAsync(request);
            if (cuenta.BancoId != data.Banco.Id ||
                !string.Equals(cuenta.NumeroCuenta, data.NumeroCuenta, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(cuenta.MonedaCodigo, data.MonedaCodigo, StringComparison.OrdinalIgnoreCase))
            {
                await EnsureCuentaBancoNumeroDisponibleAsync(data.Banco.Id, data.NumeroCuenta, data.MonedaCodigo, id);
            }

            cuenta.BancoId = data.Banco.Id;
            cuenta.Descripcion = data.Descripcion;
            cuenta.TipoCuenta = data.TipoCuenta;
            cuenta.MonedaCodigo = data.MonedaCodigo;
            cuenta.NumeroCuenta = data.NumeroCuenta;
            cuenta.Cbu = data.Cbu;
            cuenta.AliasCbu = data.AliasCbu;
            cuenta.CuentaContableId = data.CuentaContableId;
            cuenta.Activa = request.Activa;
            cuenta.FechaModificacion = DateTime.UtcNow;
            cuenta.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            return (await GetCuentaBancariaAsync(cuenta.Id))!;
        }

        private IQueryable<CuentaBancariaEmpresa> GetCuentaQuery(bool tracking)
        {
            var query = _db.CuentasBancariasEmpresa
                .Include(c => c.Banco)
                .Include(c => c.CuentaContable)
                .AsQueryable();

            return tracking ? query : query.AsNoTracking();
        }

        private async Task<ValidatedCuentaRequest> ValidateCuentaRequestAsync(UpsertCuentaBancariaEmpresaRequest request)
        {
            var banco = await _db.BancosCobranza.FirstOrDefaultAsync(b => b.Id == request.BancoId);
            if (banco == null)
            {
                throw new InvalidOperationException("El banco indicado no existe.");
            }

            if (!banco.Activo)
            {
                throw new InvalidOperationException("La cuenta bancaria debe estar asociada a un banco activo.");
            }

            var descripcion = NormalizeRequired(request.Descripcion, "La descripcion de la cuenta bancaria es obligatoria.");
            var tipoCuenta = NormalizeTipoCuenta(request.TipoCuenta);
            var moneda = NormalizeCurrency(request.MonedaCodigo);
            var numeroCuenta = NormalizeRequired(request.NumeroCuenta, "El numero de cuenta es obligatorio.");
            var cuentaContable = await _db.CuentasContables.FirstOrDefaultAsync(c => c.Id == request.CuentaContableId);
            if (cuentaContable == null)
            {
                throw new InvalidOperationException("La cuenta contable indicada no existe.");
            }

            if (!cuentaContable.Activa)
            {
                throw new InvalidOperationException("La cuenta contable indicada debe estar activa.");
            }

            return new ValidatedCuentaRequest(
                banco,
                descripcion,
                tipoCuenta,
                moneda,
                numeroCuenta,
                NormalizeOptional(request.Cbu),
                NormalizeOptional(request.AliasCbu),
                cuentaContable.Id);
        }

        private async Task EnsureCodigoBancoDisponibleAsync(string codigo, int? excludeId = null)
        {
            var exists = await _db.BancosCobranza.AnyAsync(b => b.Codigo == codigo && (!excludeId.HasValue || b.Id != excludeId.Value));
            if (exists)
            {
                throw new InvalidOperationException("Ya existe un banco con ese codigo.");
            }
        }

        private async Task EnsureCuentaBancoNumeroDisponibleAsync(int bancoId, string numeroCuenta, string moneda, int? excludeId = null)
        {
            var exists = await _db.CuentasBancariasEmpresa.AnyAsync(c =>
                c.BancoId == bancoId &&
                c.NumeroCuenta == numeroCuenta &&
                c.MonedaCodigo == moneda &&
                (!excludeId.HasValue || c.Id != excludeId.Value));

            if (exists)
            {
                throw new InvalidOperationException("Ya existe una cuenta bancaria para ese banco, numero y moneda.");
            }
        }

        private static BancoResponse MapBanco(BancoCobranza banco)
        {
            return new BancoResponse
            {
                Id = banco.Id,
                Codigo = banco.Codigo,
                Nombre = banco.Nombre,
                Activo = banco.Activo,
                FechaAlta = banco.FechaAlta,
                UsuarioAlta = banco.UsuarioAlta,
                FechaModificacion = banco.FechaModificacion,
                UsuarioModificacion = banco.UsuarioModificacion
            };
        }

        private static CuentaBancariaEmpresaResponse MapCuenta(CuentaBancariaEmpresa cuenta)
        {
            return new CuentaBancariaEmpresaResponse
            {
                Id = cuenta.Id,
                BancoId = cuenta.BancoId,
                BancoCodigo = cuenta.Banco.Codigo,
                BancoNombre = cuenta.Banco.Nombre,
                Descripcion = cuenta.Descripcion,
                TipoCuenta = cuenta.TipoCuenta,
                MonedaCodigo = cuenta.MonedaCodigo,
                NumeroCuenta = cuenta.NumeroCuenta,
                Cbu = cuenta.Cbu,
                AliasCbu = cuenta.AliasCbu,
                CuentaContableId = cuenta.CuentaContableId,
                CuentaContableCodigo = cuenta.CuentaContable.Codigo,
                CuentaContableNombre = cuenta.CuentaContable.Nombre,
                Activa = cuenta.Activa,
                FechaAlta = cuenta.FechaAlta,
                UsuarioAlta = cuenta.UsuarioAlta,
                FechaModificacion = cuenta.FechaModificacion,
                UsuarioModificacion = cuenta.UsuarioModificacion
            };
        }

        private static string NormalizeCodigo(string? value)
        {
            var normalized = NormalizeRequired(value, "El codigo del banco es obligatorio.").ToUpperInvariant();
            if (normalized.Length > 50)
            {
                throw new InvalidOperationException("El codigo del banco no puede superar 50 caracteres.");
            }

            return normalized;
        }

        private static string NormalizeTipoCuenta(string? value)
        {
            var normalized = NormalizeRequired(value, "El tipo de cuenta es obligatorio.").ToUpperInvariant();
            if (!TiposCuentaValidos.Contains(normalized))
            {
                throw new InvalidOperationException("El tipo de cuenta bancaria no es valido.");
            }

            return normalized;
        }

        private static string NormalizeCurrency(string? value)
        {
            var normalized = NormalizeRequired(value, "La moneda es obligatoria.").ToUpperInvariant();
            if (!MonedasValidas.Contains(normalized))
            {
                throw new InvalidOperationException("La moneda debe ser ARS o USD.");
            }

            return normalized;
        }

        private static string? NormalizeCurrencyFilter(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return NormalizeCurrency(value);
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

        private static string? NormalizeOptional(string? value)
        {
            var normalized = value?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private sealed record ValidatedCuentaRequest(
            BancoCobranza Banco,
            string Descripcion,
            string TipoCuenta,
            string MonedaCodigo,
            string NumeroCuenta,
            string? Cbu,
            string? AliasCbu,
            int CuentaContableId);
    }
}
