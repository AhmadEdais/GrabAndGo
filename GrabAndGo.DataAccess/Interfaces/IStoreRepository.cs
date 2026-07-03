namespace GrabAndGo.DataAccess.Interfaces;

public interface IStoreRepository
{
    Task<bool> StoreExistsAsync(int storeId);
}
