using BudgetControl.Api.Data;
using BudgetControl.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services
{
    public class ExternalDataService : IExternalDataService
    {
        private readonly ExternalDbContext _context;

        public ExternalDataService(ExternalDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Client>> GetClientsAsync()
        {
            return await _context.Clients.AsNoTracking().ToListAsync();
        }

        public async Task<Client?> GetClientByIdAsync(int clienteId)
        {
            return await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.IdCliente == clienteId);
        }

        public async Task<IEnumerable<Obra>> GetObrasByClientAsync(int clienteId)
        {
            return await _context.Obras.Where(o => o.ClienteId == clienteId).AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Obra>> GetObrasAsync()
        {
            return await _context.Obras.AsNoTracking().ToListAsync();
        }

        public async Task<Obra?> GetObraByIdAsync(int obraId)
        {
            return await _context.Obras.AsNoTracking().FirstOrDefaultAsync(o => o.IdObra == obraId);
        }
    }
}
