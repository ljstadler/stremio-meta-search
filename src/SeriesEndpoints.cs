using System.Text.Encodings.Web;
using Generated.TVDB;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

public static class SeriesEndpoints
{
    private static readonly string stremioGenreBaseUrl = "stremio:///discover/https%3A%2F%2Fv3-cinemeta.strem.io%2Fmanifest.json/series/top?genre=";

    private static readonly string stremioSearchBaseUrl = "stremio:///search?search=";

    private static readonly string[] stremioGenres = [
        "Action",
        "Adventure",
        "Animation",
        "Biography",
        "Comedy",
        "Crime",
        "Documentary",
        "Drama",
        "Family",
        "Fantasy",
        "History",
        "Horror",
        "Mystery",
        "Romance",
        "Sci-Fi",
        "Sport",
        "Thriller",
        "War",
        "Western",
        "Reality-TV",
        "Talk-Show",
        "Game-Show",
    ];

    private static readonly Dictionary<string, string> genreCorrections = new()
    {
        ["Science Fiction"] = "Sci-Fi",
        ["Reality"] = "Reality-TV",
        ["Talk Show"] = "Talk-Show",
        ["Game Show"] = "Game-Show"
    };

    private static readonly TVDBClient tvdbClient = new(new HttpClientRequestAdapter(new AnonymousAuthenticationProvider()));

    public static void Map(WebApplication app)
    {
        app.MapGet("/catalog/series/meta_search_series/{search}", async (TVDBConfig config, string search) =>
        {
            try
            {
                var tvdb = await tvdbClient.Search.GetAsSearchGetResponseAsync(r =>
                {
                    r.Headers.Add("Authorization", $"Bearer {config.TvdbApiAccessToken}");
                    r.QueryParameters.Query = search[7..^5];
                    r.QueryParameters.Type = "series";
                });
                return new MetaPreviewsResponse()
                {
                    Metas = tvdb?.Data?
                        .Where(s => s.TvdbId != null && s.Name != null)
                        .Select(s => new MetaPreview
                        {
                            Id = $"tvdb:{s.TvdbId}",
                            Type = "series",
                            Name = s.Name!,
                            Poster = s.Thumbnail,
                        }).ToList() ?? []
                };
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "TVDB");
                return new MetaPreviewsResponse();
            }
        });

        app.MapGet("/meta/series/{id}", async (TVDBConfig config, string id) =>
        {
            try
            {
                var tvdb = await tvdbClient.Series[int.Parse(id[5..^5])].Extended.GetAsExtendedGetResponseAsync(r =>
            {
                r.Headers.Add("Authorization", $"Bearer {config.TvdbApiAccessToken}");
                r.QueryParameters.MetaAsGetMetaQueryParameterType = Generated.TVDB.Series.Item.Extended.GetMetaQueryParameterType.Episodes;
            });
                if (tvdb?.Data != null)
                {
                    return new MetaDetailResponse()
                    {
                        Meta = new()
                        {
                            Id = $"tvdb:{tvdb.Data.Id}",
                            Type = "series",
                            Name = tvdb.Data.Name!,
                            Poster = tvdb.Data.Artworks?.Find(a => a.Type == 2)?.Thumbnail,
                            Background = tvdb.Data.Artworks?.Find(a => a.Type == 3)?.Image,
                            Logo = tvdb.Data.Artworks?.Find(a => a.Type == 23)?.Thumbnail,
                            Description = tvdb.Data.Overview,
                            ReleaseInfo = tvdb.Data.Status?.Id switch
                            {
                                1 => $"{tvdb.Data.FirstAired?[..4]}-",
                                2 => $"{tvdb.Data.FirstAired?[..4]}-{tvdb.Data.LastAired?[..4]}",
                                _ => null
                            },
                            Released = tvdb.Data.FirstAired != null ? $"{tvdb.Data.FirstAired}T00:00:00.000Z" : null,
                            Runtime = tvdb.Data.AverageRuntime != null ? $"{tvdb.Data.AverageRuntime}m" : null,
                            BehaviorHints = new()
                            {
                                HasScheduledVideos = tvdb.Data.Status?.Id == 1,
                            },
                            Links = [
                                ..tvdb.Data.Genres?
                                .Where(g => g.Name != null && (genreCorrections.ContainsKey(g.Name) || stremioGenres.Contains(g.Name)))
                                .Select(g => new MetaLink
                                {
                                    Name = (g.Name != null ? genreCorrections.GetValueOrDefault(g.Name) : null) ?? g.Name!,
                                    Category = "Genres",
                                    Url = $"{stremioGenreBaseUrl}{g.Name}"
                                }).ToList() ?? [],
                            ..tvdb.Data.Characters?
                                .Where(c => c.PersonName != null && c.PeopleType == "Actor")
                                .Take(5)
                                .Select(c => new MetaLink
                                {
                                    Name = c.PersonName!,
                                    Category = "Cast",
                                    Url = $"{stremioSearchBaseUrl}{UrlEncoder.Default.Encode(c.Name!)}"
                                }).ToList() ?? []
                            ],
                            Videos = tvdb.Data.Episodes?
                                .Where(e => e.Id != null && e.Name != null && e.SeasonNumber != null && e.SeasonNumber != 0 && e.Number != null)
                                .Select(e => new MetaVideo
                                {
                                    Id = $"tvdb:{tvdb.Data.Id}:{e.SeasonNumber}:{e.Number}",
                                    Title = e.Name!,
                                    Season = (int)e.SeasonNumber!,
                                    Episode = (int)e.Number!,
                                    Overview = e.Overview,
                                    Released = e.Aired != null ? $"{e.Aired}T00:00:00.000Z" : "",
                                    Thumbnail = e.Image != null
                                        ? $"https://artworks.thetvdb.com{e.Image}"
                                        : "https://www.thetvdb.com/images/missing/episode.jpg",
                                }).ToList() ?? []
                        }
                    };
                }
                else
                {
                    return new MetaDetailResponse();
                }
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "TVDB");
                return new MetaDetailResponse();
            }
        });
    }
}