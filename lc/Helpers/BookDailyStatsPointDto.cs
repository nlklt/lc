namespace lc.Helpers;

public sealed record BookDailyStatsPointDto(
    DateTime Day,
    int ViewsCount,
    int RatingsCount,
    int CommentsCount);