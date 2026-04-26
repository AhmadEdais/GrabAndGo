namespace GrabAndGo.Api.Security
{
    /// <summary>
    /// Authorizes hardware/edge devices via a pre-shared API key.
    /// Apply at controller or action level: <c>[RequireApiKey("Gate")]</c>.
    /// The expected key is read from <c>HardwareAuth:{role}ApiKey</c> in configuration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RequireApiKeyAttribute : Attribute, IAuthorizationFilter
    {
        private const string HeaderName = "X-Api-Key";
        private readonly string _role;

        public RequireApiKeyAttribute(string role)
        {
            _role = role;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var expectedKey = config[$"HardwareAuth:{_role}ApiKey"];

            // 1. If the server itself has no key configured, fail closed.
            //    Better to break loudly in dev than silently allow everyone in prod.
            if (string.IsNullOrWhiteSpace(expectedKey))
            {
                context.Result = new ObjectResult(new { message = $"Server is missing HardwareAuth:{_role}ApiKey configuration." })
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
                return;
            }

            // 2. Caller must include the header.
            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey)
                || string.IsNullOrWhiteSpace(providedKey))
            {
                context.Result = new UnauthorizedObjectResult(new { message = $"Missing {HeaderName} header." });
                return;
            }

            // 3. Constant-time comparison defends against timing-based key recovery.
            //    Equal-length check first because FixedTimeEquals requires equal lengths.
            var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);
            var providedBytes = Encoding.UTF8.GetBytes(providedKey.ToString());

            if (expectedBytes.Length != providedBytes.Length
                || !CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Invalid API key." });
                return;
            }

            // Authorized — let the request proceed.
        }
    }
}