public class MetaPreview
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public required string Name { get; set; }
    public string? Poster { get; set; }
    public string? Background { get; set; }
    public string? Logo { get; set; }
    public string? Description { get; set; }
}

public class MetaPreviewsResponse
{
    public List<MetaPreview>? Metas { get; set; }
}

public class MetaBehaviorHints
{
    public bool? HasScheduledVideos { get; set; }
}

public class MetaLink
{
    public required string Name { get; set; }
    public required string Category { get; set; }
    public required string Url { get; set; }
}

public class MetaVideo
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required int Season { get; set; }
    public required int Episode { get; set; }
    public string? Overview { get; set; }
    public required string Released { get; set; }
    public string? Thumbnail { get; set; }
}

public class MetaDetail : MetaPreview
{
    public string? ReleaseInfo { get; set; }
    public string? Released { get; set; }
    public string? Runtime { get; set; }
    public MetaBehaviorHints? BehaviorHints { get; set; }
    public List<MetaLink>? Links { get; set; }
    public List<MetaVideo>? Videos { get; set; }
}

public class MetaDetailResponse
{
    public MetaDetail? Meta { get; set; }
}