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
            var userId =  GetVerifiedUserId();
            var owns = await _sessionService.DoesUserOwnActiveSessionAsync(userId, sessionId);
            if (!owns)
            {
                throw new HubException("Cannot subscribe to a session you do not own.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
        }

        public async Task UnsubscribeFromSession(int sessionId)
        {
            var userId =  GetVerifiedUserId();

            var owns = await _sessionService.DoesUserOwnActiveSessionAsync(userId, sessionId);
            if (!owns)
            {
                throw new HubException("Cannot subscribe to a session you do not own.");
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
        }
        public async Task SubscribeToDemoFeed()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "DemoControllers");
        }

        public async Task UnsubscribeFromDemoFeed()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "DemoControllers");
        }
        private int GetVerifiedUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                throw new HubException("Invalid token identity.");
            }
            return userId;
        }
    }
}
