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
            // 1. Identity: must come from the authenticated JWT, never from a method parameter.
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                throw new HubException("Invalid token identity.");
            }

            // 2. Ownership: only the session's owner may subscribe to its live cart updates.
            var owns = await _sessionService.DoesUserOwnActiveSessionAsync(userId, sessionId);
            if (!owns)
            {
                throw new HubException("Cannot subscribe to a session you do not own.");
            }

            // 3. Authorized — join the per-session group.
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
        }

        public async Task UnsubscribeFromSession(int sessionId)
        {
            // No ownership check needed: RemoveFromGroupAsync only affects the caller's own
            // connection, so an unauthorized caller can at worst no-op themselves.
            // [Authorize] on the class still blocks anonymous calls.
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Session_{sessionId}");
        }
    }
}
