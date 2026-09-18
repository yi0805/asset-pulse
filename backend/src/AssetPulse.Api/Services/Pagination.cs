namespace AssetPulse.Api.Services;

public readonly record struct Pagination(int Page, int PageSize)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    public long Skip => ((long)Page - 1) * PageSize;

    public static bool TryCreate(
        int? requestedPage,
        int? requestedPageSize,
        out Pagination pagination,
        out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();
        var page = requestedPage ?? DefaultPage;
        var pageSize = requestedPageSize ?? DefaultPageSize;

        if (page < 1)
        {
            errors["page"] = ["Page must be a positive integer."];
        }

        if (pageSize < 1 || pageSize > MaximumPageSize)
        {
            errors["pageSize"] = [$"Page size must be between 1 and {MaximumPageSize}."];
        }

        pagination = new Pagination(page, pageSize);
        return errors.Count == 0;
    }
}
