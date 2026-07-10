using BudgetControl.Api.Models;

namespace BudgetControl.Api.Services
{
    public interface IExternalDataService
    {
            Task<IEnumerable<Client>> GetClientsAsync();
        Task<Client?> GetClientByIdAsync(int clienteId);
        Task<IEnumerable<Obra>> GetObrasByClientAsync(int clienteId);
        Task<IEnumerable<Obra>> GetObrasAsync();
        Task<Obra?> GetObraByIdAsync(int obraId);
    }
}
