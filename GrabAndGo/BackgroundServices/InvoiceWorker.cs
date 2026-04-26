namespace GrabAndGo.Api.BackgroundServices
{
    public class InvoiceWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InvoiceWorker> _logger;

        private readonly TimeSpan _pollInterval;
        private readonly int _batchSize;

        public InvoiceWorker(IServiceScopeFactory scopeFactory, ILogger<InvoiceWorker> logger, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            int pollSeconds = int.TryParse(config["Invoice:PollSeconds"], out var s) && s > 0 ? s : 5;
            _pollInterval = TimeSpan.FromSeconds(pollSeconds);
            _batchSize = int.TryParse(config["Invoice:BatchSize"], out var b) && b > 0 ? b : 10;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "InvoiceWorker started. Polling every {Interval}s, batch size {BatchSize}.",
                _pollInterval.TotalSeconds, _batchSize);

            // Short initial delay so DB / DI container fully settles before first tick.
            try { await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync();
                }
                catch (Exception ex)
                {
                    // Swallow and log — never crash the worker. Pending invoices stay NULL and
                    // will be retried on the next tick.
                    _logger.LogError(ex, "InvoiceWorker batch failed — will retry on next cycle.");
                }

                try { await Task.Delay(_pollInterval, stoppingToken); }
                catch (TaskCanceledException) { break; }
            }

            _logger.LogInformation("InvoiceWorker stopping.");
        }

        private async Task ProcessBatchAsync()
        {
            // Scoped services (IInvoiceService / repositories) require a fresh scope per tick.
            using var scope = _scopeFactory.CreateScope();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

            var pending = await invoiceService.GetPendingInvoicesAsync(_batchSize);
            if (pending == null || pending.Count == 0) return;

            _logger.LogInformation("Found {Count} pending invoice(s) to generate.", pending.Count);

            foreach (var item in pending)
            {
                try
                {
                    var result = await invoiceService.GenerateInvoiceAsync(item.TransactionId);
                    if (result.IsSuccess)
                    {
                        _logger.LogInformation(
                            "Invoice generated for TransactionId={TransactionId}: {Path}",
                            item.TransactionId, result.PdfUrlOrPath);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Invoice generation unsuccessful for TransactionId={TransactionId}: {Message}",
                            item.TransactionId, result.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Exception generating invoice for TransactionId={TransactionId}", item.TransactionId);
                }
            }
        }
    }
}
