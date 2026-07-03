namespace GrabAndGo.DataAccess.Repositories;

public class StoreRepository : IStoreRepository
{
    private readonly SqlExecutor _executor;

    public StoreRepository(SqlExecutor executor)
    {
        _executor = executor;
    }

    public async Task<bool> StoreExistsAsync(int storeId)
    {
        var result = await _executor.ExecuteScalarAsync<int>(
            "SP_DoesStoreExist",
            new { StoreId = storeId }
        );
        return result == 1;
    }
}