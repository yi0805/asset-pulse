using AssetPulse.Api.Domain;

namespace AssetPulse.Api.Contracts;

public sealed class AssetListQuery
{
    public string? Search { get; init; }

    public string? Type { get; init; }

    public string? Location { get; init; }

    public AssetStatus? Status { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}

public sealed class AlarmListQuery
{
    public int? AssetId { get; init; }

    public AlarmSeverity? Severity { get; init; }

    public AlarmStatus? Status { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}

public sealed class PaginationQuery
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }
}
