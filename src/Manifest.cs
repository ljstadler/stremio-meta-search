public static class Manifest
{
    public const string manifest = """
    {
        "id": "meta.search",
        "name": "Meta Search",
        "description": "Search for movies via TMDB and series via TVDB",
        "version": "1.0.0",
        "resources": ["catalog", "meta"],
        "types": ["movie", "series"],
        "idPrefixes": ["tmdb:", "tvdb:"],
        "catalogs": [
            {
                "type": "movie",
                "id": "meta_search_movie",
                "name": "TMDB",
                "extra": [
                    {
                        "name": "search",
                        "isRequired": true
                    }
                ]
            },
            {
                "type": "series",
                "id": "meta_search_series",
                "name": "TVDB",
                "extra": [
                    {
                        "name": "search",
                        "isRequired": true
                    }
                ]
            }
        ]
    }
    """;
}