using VCheck.Lib.Data;

namespace VCheckViewerAPI.Services
{
    public class BackgroundServices : BackgroundService
    {
        private readonly ILogger<BackgroundServices> _logger;
        private const long MemoryThresholdBytes = 200L * 1024L * 1024L;

        public BackgroundServices(ILogger<BackgroundServices> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
            //using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    //_logger.LogInformation("Timer executed at {Time}", DateTime.Now);

                    // Your code here
                    DoSomethingAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    //_logger.LogError(ex, "Background task failed");
                }
            }
        }

        private void DoSomethingAsync(CancellationToken cancellationToken)
        {

            try
            {
                //ScheduledTestRepository.refres
                long currentMemory = GC.GetTotalMemory(forceFullCollection: false);
                //Console.WriteLine($"Current memory usage: {currentMemory / (1024 * 1024)} MB");

                if (currentMemory > MemoryThresholdBytes)
                {
                    //Console.WriteLine("Memory threshold exceeded — running full GC...");

                    GC.Collect(
                        GC.MaxGeneration,                // Collect all generations
                        GCCollectionMode.Forced,         // Force collection
                        blocking: true,                  // Wait until done
                        compacting: true                  // Compact heap
                    );

                    GC.WaitForPendingFinalizers();
                    //Console.WriteLine("Full GC completed.");
                }
                else
                {
                    //Console.WriteLine("Memory usage below threshold — skipping GC.");
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error during GC: {ex.Message}");
            }
        }
    }
}
