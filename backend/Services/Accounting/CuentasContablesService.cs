using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Accounting;
using BudgetControl.Api.Models.Accounting;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Accounting
{
    public class CuentasContablesService : ICuentasContablesService
    {
        private static readonly HashSet<string> TiposCuentaValidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "Activo",
            "Pasivo",
            "Patrimonio Neto",
            "Ingreso",
            "Egreso"
        };

        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;

        public CuentasContablesService(AppDbContext db, IUserContext userContext)
        {
            _db = db;
            _userContext = userContext;
        }

        public async Task<IEnumerable<CuentaContableResponse>> GetCuentasAsync(CuentaContableFilter filter)
        {
            var query = _db.CuentasContables.AsNoTracking().AsQueryable();

            var codigo = NormalizeCodigoFilter(filter.Codigo);
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                query = query.Where(c => c.Codigo.Contains(codigo));
            }

            var nombre = filter.Nombre?.Trim();
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                query = query.Where(c => c.Nombre.ToLower().Contains(nombre.ToLower()));
            }

            var tipoCuenta = NormalizeTipoCuenta(filter.TipoCuenta);
            if (!string.IsNullOrWhiteSpace(tipoCuenta))
            {
                query = query.Where(c => c.TipoCuenta == tipoCuenta);
            }

            if (filter.Activa.HasValue)
            {
                query = query.Where(c => c.Activa == filter.Activa.Value);
            }

            var cuentas = await query
                .OrderBy(c => c.Codigo)
                .ToListAsync();

            return cuentas.Select(Map);
        }

        public async Task<CuentaContableResponse?> GetCuentaAsync(int id)
        {
            var cuenta = await _db.CuentasContables
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            return cuenta == null ? null : Map(cuenta);
        }

        public async Task<CuentaContableResponse> CreateCuentaAsync(CreateCuentaContableRequest request)
        {
            var codigo = NormalizeCodigo(request.Codigo);
            var nombre = NormalizeRequiredText(request.Nombre, "El nombre de la cuenta es obligatorio.");
            var tipoCuenta = NormalizeRequiredTipoCuenta(request.TipoCuenta);

            await EnsureCodigoDisponibleAsync(codigo);

            var cuenta = new CuentaContable
            {
                Codigo = codigo,
                Nombre = nombre,
                TipoCuenta = tipoCuenta,
                Activa = true,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            _db.CuentasContables.Add(cuenta);
            await _db.SaveChangesAsync();

            return Map(cuenta);
        }

        public async Task<CuentaContableResponse> UpdateCuentaAsync(int id, UpdateCuentaContableRequest request)
        {
            var cuenta = await _db.CuentasContables.FirstOrDefaultAsync(c => c.Id == id);
            if (cuenta == null)
            {
                throw new KeyNotFoundException("Cuenta contable no encontrada.");
            }

            var codigo = NormalizeCodigo(request.Codigo);
            var nombre = NormalizeRequiredText(request.Nombre, "El nombre de la cuenta es obligatorio.");
            var tipoCuenta = NormalizeRequiredTipoCuenta(request.TipoCuenta);

            if (!string.Equals(cuenta.Codigo, codigo, StringComparison.OrdinalIgnoreCase))
            {
                await EnsureCodigoDisponibleAsync(codigo, id);
                cuenta.Codigo = codigo;
            }

            cuenta.Nombre = nombre;
            cuenta.TipoCuenta = tipoCuenta;
            cuenta.Activa = request.Activa;

            await _db.SaveChangesAsync();
            return Map(cuenta);
        }

        public async Task<bool> DarDeBajaAsync(int id)
        {
            var cuenta = await _db.CuentasContables.FirstOrDefaultAsync(c => c.Id == id);
            if (cuenta == null)
            {
                return false;
            }

            cuenta.Activa = false;
            await _db.SaveChangesAsync();
            return true;
        }

        private async Task EnsureCodigoDisponibleAsync(string codigo, int? excludeId = null)
        {
            var exists = await _db.CuentasContables
                .AnyAsync(c => c.Codigo == codigo && (!excludeId.HasValue || c.Id != excludeId.Value));

            if (exists)
            {
                throw new InvalidOperationException("Ya existe una cuenta contable con ese codigo.");
            }
        }

        private static string NormalizeCodigo(string? codigo)
        {
            var normalized = codigo?.Trim().ToUpperInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidOperationException("El codigo de cuenta es obligatorio.");
            }
            return normalized;
        }

        private static string? NormalizeCodigoFilter(string? codigo)
        {
            return string.IsNullOrWhiteSpace(codigo) ? null : codigo.Trim().ToUpperInvariant();
        }

        private static string NormalizeRequiredText(string? value, string errorMessage)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidOperationException(errorMessage);
            }
            return normalized;
        }

        private static string NormalizeRequiredTipoCuenta(string? tipoCuenta)
        {
            var normalized = NormalizeTipoCuenta(tipoCuenta);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidOperationException("El tipo de cuenta es obligatorio.");
            }

            if (!TiposCuentaValidos.Contains(normalized))
            {
                throw new InvalidOperationException("El tipo de cuenta no es valido.");
            }

            return normalized;
        }

        private static string NormalizeTipoCuenta(string? tipoCuenta)
        {
            if (string.IsNullOrWhiteSpace(tipoCuenta))
            {
                return string.Empty;
            }

            var normalized = tipoCuenta.Trim();
            return TiposCuentaValidos.FirstOrDefault(t => string.Equals(t, normalized, StringComparison.OrdinalIgnoreCase))
                ?? normalized;
        }

        private static CuentaContableResponse Map(CuentaContable cuenta)
        {
            return new CuentaContableResponse
            {
                Id = cuenta.Id,
                Codigo = cuenta.Codigo,
                Nombre = cuenta.Nombre,
                TipoCuenta = cuenta.TipoCuenta,
                Activa = cuenta.Activa,
                FechaAlta = cuenta.FechaAlta,
                UsuarioAlta = cuenta.UsuarioAlta
            };
        }
    }
}
