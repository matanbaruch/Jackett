using System.Threading.Tasks;

namespace Jackett.Common.Services.Interfaces
{
    public interface ITelegramNotificationService
    {
        Task<bool> SendNotificationAsync(string message);
        Task<bool> TestConnectionAsync();
        bool IsConfigured { get; }
    }
}
