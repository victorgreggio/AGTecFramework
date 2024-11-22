namespace AGTec.Common.CQRS.Queries;

public interface IPaginatedQuery<out TResult> : IQuery<TResult>
{
    int Page { get; }
    int PageSize { get; }
}