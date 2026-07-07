namespace GrabAndGo.DataAccess.Interfaces;

public interface IBroadcastSessionToDemo
{
    Task BroadcastSessionEnteredAsync(int sessionId, int storeId, DateTime startedAt);
}
