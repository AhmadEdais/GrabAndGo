namespace GrabAndGo.Services.Interfaces;

public  interface IStoreService
{
    Task<bool> StoreExistsAsync(int storeId);
}
