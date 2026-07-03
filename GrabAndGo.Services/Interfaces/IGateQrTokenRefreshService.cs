namespace GrabAndGo.Services.Interfaces;

public interface IGateQrTokenRefreshService
{
    Task RefreshQrTokenAsync(int storeId);
}
