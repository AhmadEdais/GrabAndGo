
namespace GrabAndGo.Services.Implementations;

public class StoreService :IStoreService
{
    private readonly IStoreRepository _storeRepository;
    public StoreService(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository;
    }
    public async Task<bool> StoreExistsAsync(int storeId)
    {

        return await _storeRepository.StoreExistsAsync(storeId);
    }
}
