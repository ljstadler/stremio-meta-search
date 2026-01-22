using System.Text.Encodings.Web;
using Generated.TMDB;
using Generated.TMDB.Three.Movie.Item.Images;
using Generated.TMDB.Three.Tv.Item.Credits;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Http.HttpClientLibrary;

public static class MovieEndpoints
{
    private static readonly string tmdbImageBaseUrl = "https://image.tmdb.org/t/p/";

    private static readonly string stremioGenreBaseUrl = "stremio:///discover/https%3A%2F%2Fv3-cinemeta.strem.io%2Fmanifest.json/movie/top?genre=";

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
    ];

    private static readonly TMDBClient tmdbClient = new(new HttpClientRequestAdapter(new AnonymousAuthenticationProvider()));

    public static void Map(WebApplication app)
    {
        app.MapGet("/catalog/movie/meta_search_movie/{search}", async (IOptions<Config> config, string search) =>
        {
            try
            {
                var tmdb = await tmdbClient.Three.Search.Movie.GetAsMovieGetResponseAsync(r =>
                {
                    r.Headers.Add("Authorization", $"Bearer {config.Value.TmdbApiAccessToken}");
                    r.QueryParameters.Query = search[7..^5];
                    r.QueryParameters.Language = "en-US";
                });
                return new MetaPreviewsResponse()
                {
                    Metas = tmdb?.Results?
                        .Where(m => m.Id != null && m.Title != null)
                        .Select(m => new MetaPreview
                        {
                            Id = $"tmdb:{m.Id}",
                            Type = "movie",
                            Name = m.Title!,
                            Poster = m.PosterPath != null ? $"{tmdbImageBaseUrl}w300{m.PosterPath}" : null,
                        }).ToList() ?? []
                };
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "TMDB");
                return new MetaPreviewsResponse();
            }
        });

        app.MapGet("/meta/movie/{id}", async (IOptions<Config> config, string id) =>
        {
            try
            {
                var tmdb = await tmdbClient.Three.Movie[int.Parse(id[5..^5])].GetAsWithMovie_GetResponseAsync(r =>
                {
                    r.Headers.Add("Authorization", $"Bearer {config.Value.TmdbApiAccessToken}");
                    r.QueryParameters.AppendToResponse = "credits,images";
                    r.QueryParameters.Language = "en-US";
                });
                if (tmdb != null)
                {
                    CreditsGetResponse? credits = null;
                    ImagesGetResponse? images = null;
                    if (tmdb.AdditionalData.TryGetValue("credits", out var creditsRaw) && creditsRaw is UntypedNode creditsNode)
                    {
                        string creditsJson = await KiotaJsonSerializer.SerializeAsStringAsync(creditsNode);
                        credits = await KiotaJsonSerializer.DeserializeAsync(creditsJson, CreditsGetResponse.CreateFromDiscriminatorValue);
                    }
                    if (tmdb.AdditionalData.TryGetValue("images", out var imagesRaw) && imagesRaw is UntypedNode imagesNode)
                    {
                        string imagesJson = await KiotaJsonSerializer.SerializeAsStringAsync(imagesNode);
                        images = await KiotaJsonSerializer.DeserializeAsync(imagesJson, ImagesGetResponse.CreateFromDiscriminatorValue);
                    }
                    return new MetaDetailResponse()
                    {
                        Meta = new()
                        {
                            Id = $"tmdb:{tmdb.Id}",
                            Type = "movie",
                            Name = tmdb.Title!,
                            Poster = tmdb.PosterPath != null ? $"{tmdbImageBaseUrl}w300{tmdb.PosterPath}" : null,
                            Background = tmdb.BackdropPath != null ? $"{tmdbImageBaseUrl}original{tmdb.BackdropPath}" : null,
                            Logo = images?.Logos?.Count > 0 ? $"{tmdbImageBaseUrl}w500{images.Logos[0].FilePath}" : null,
                            Description = tmdb.Overview,
                            ReleaseInfo = tmdb.ReleaseDate?[..4],
                            Released = tmdb.ReleaseDate != null ? $"{tmdb.ReleaseDate}T00:00:00.000Z" : null,
                            Runtime = tmdb.Runtime != null ? $"{tmdb.Runtime}m" : null,
                            Links = [
                                ..tmdb.Genres?
                                .Where(g => g.Name != null && stremioGenres.Contains(g.Name))
                                .Select(g => new MetaLink
                                {
                                    Name = g.Name == "Science Fiction" ? "Sci-Fi" : g.Name!,
                                    Category = "Genres",
                                    Url = $"{stremioGenreBaseUrl}{g.Name}"
                                }).ToList() ?? [],
                            ..credits?.Cast?
                                .Where(c => c.Name != null)
                                .Take(5)
                                .Select(c => new MetaLink
                                {
                                    Name = c.Name!,
                                    Category = "Cast",
                                    Url = $"{stremioSearchBaseUrl}{UrlEncoder.Default.Encode(c.Name!)}"
                                }).ToList() ?? [],
                            ..credits?.Crew?
                                .Where(c => c.Name != null && c.Job == "Director")
                                .Take(5)
                                .Select(c => new MetaLink
                                {
                                    Name = c.Name!,
                                    Category = "Directors",
                                    Url = $"{stremioSearchBaseUrl}{UrlEncoder.Default.Encode(c.Name!)}"
                                }).ToList() ?? []
                            ]
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
                app.Logger.LogError(ex, "TMDB");
                return new MetaDetailResponse();
            }
        });
    }
}
