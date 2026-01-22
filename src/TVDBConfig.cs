using Generated.TVDB;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

public class TVDBConfig
{
    public string TvdbApiAccessToken { get; set; } = string.Empty;
}

public class TVDBConfigRefreshWorker(TVDBConfig tvdbConfig, IOptions<Config> config, ILogger<TVDBConfigRefreshWorker> logger) : BackgroundService
{
    private static readonly TVDBClient tvdbClient = new(new HttpClientRequestAdapter(new AnonymousAuthenticationProvider()));

    private async Task RefreshTvdbApiAccessToken()
    {
        logger.LogInformation("Refreshing TVDB API access token");
        var tvdb = await tvdbClient.Login.PostAsLoginPostResponseAsync(new()
        {
            Apikey = config.Value.TvdbApiKey
        });
        if (tvdb?.Data?.Token != null)
        {
            tvdbConfig.TvdbApiAccessToken = tvdb.Data.Token;
            logger.LogInformation("Refreshed TVDB API access token");
        }
        else
        {
            throw new Exception();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RefreshTvdbApiAccessToken();
                await Task.Delay(TimeSpan.FromDays(30), cancellationToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing TVDB API access token. Retrying...");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }
}