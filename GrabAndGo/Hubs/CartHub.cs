namespace GrabAndGo.Api.Hubs
{
    [Authorize]
    public class CartHub : Hub
    {
        private readonly ISessionService _sessionService;

        public CartHub(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public async Task SubscribeToSession(int sessionId)
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                throw new HubException("Invalid token identity.");
            }

            var owns = await _sessionService.DoesUserOwnActiveSessionAsync(userId, sessionId);
            if (!owns)
            {
                throw new HubException("Cannot subscribe to a session you do not own.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
        }

        public async Task UnsubscribeFromSession(int sessionId)
        {

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
        }
    }
}
