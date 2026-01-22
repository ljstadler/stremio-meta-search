using System.ComponentModel.DataAnnotations;

public class Config
{
    [ConfigurationKeyName("ADDON_PASSWORD")]
    public string? AddonPassword { get; set; } = null;

    [ConfigurationKeyName("BASE_URL")]
    public string BaseUrl { get; set; } = "http://localhost:8080";

    [ConfigurationKeyName("CACHE_TTL")]
    public int CacheTtl { get; set; } = 10800;

    [ConfigurationKeyName("TMDB_API_ACCESS_TOKEN"), Required(AllowEmptyStrings = false)]
    public string TmdbApiAccessToken { get; set; } = string.Empty;

    [ConfigurationKeyName("TVDB_API_KEY"), Required(AllowEmptyStrings = false)]
    public string TvdbApiKey { get; set; } = string.Empty;
}