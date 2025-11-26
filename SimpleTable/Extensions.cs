using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using SimpleTable.Models;

namespace SimpleTable;

public static class Extensions
{
    public static TableResult<T> ToTableResponse<T>(this IEnumerable<T> results, TableRequest tableRequest = null)
    {
        var totalResults = results.Count();
        if (tableRequest == null)
        {
            tableRequest = new TableRequest()
            {
                PageNumber = 1,
                PageSize = totalResults,
                Search = string.Empty,
                SortColumn = null,
                SortDirection = null
            };
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(tableRequest.Search))
        {
            // Search all fields that are strings or can be cast to string, should use reflection to do this
            results = results.Where(r =>
            {
                var properties = r.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.PropertyType == typeof(string) || prop.PropertyType.IsValueType)
                    {
                        var value = prop.GetValue(r)?.ToString();
                        if (value != null && value.Contains(tableRequest.Search, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }).ToList();
        }


        // Apply sorting
        if (tableRequest.SortColumn != null)
        {
            //Extensions.GetPropertyValues(results.First());
            results = tableRequest.SortDirection == "desc"
                ? results.OrderByDescending(r =>
                {
                    return r.GetType().GetProperty(tableRequest.SortColumn)?.GetValue(r);
                }).ToList()
                : results.OrderBy(r => { return r.GetType().GetProperty(tableRequest.SortColumn)?.GetValue(r); })
                    .ToList();
        }

        // Apply pagination
        results = results.Skip((tableRequest.PageNumber - 1) * tableRequest.PageSize)
            .Take(tableRequest.PageSize)
            .ToList();
        var output = new TableResult<T>()
        {
            Items = results,
            TotalCount = totalResults,
        };
        return output;
    }
    
    public static TableResult<T> ToTableResponse<T>(this IQueryable<T> query, TableRequest tableRequest = null)
    {
        var totalResults = query.Count(); // same as your original: pre-filter count

        if (tableRequest == null)
        {
            tableRequest = new TableRequest
            {
                PageNumber = 1,
                PageSize = totalResults,
                Search = string.Empty,
                SortColumn = null,
                SortDirection = null
            };
        }

        // ----- Search (only on string properties) -----
        if (!string.IsNullOrWhiteSpace(tableRequest.Search))
        {
            var search = tableRequest.Search.ToLower();
            var parameter = Expression.Parameter(typeof(T), "x");

            var stringProperties = typeof(T)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(string))
                .ToList();


            foreach (var prop in stringProperties)
            {
                var propAccess = Expression.Property(parameter, prop);
                var notNull = Expression.NotEqual(
                    propAccess,
                    Expression.Constant(null, typeof(string)));
                var toLower = Expression.Call(
                    propAccess,
                    nameof(string.ToLower),
                    Type.EmptyTypes);
                var contains = Expression.Call(
                    toLower,
                    nameof(string.Contains),
                    Type.EmptyTypes,
                    Expression.Constant(search));

                var andExpr = Expression.AndAlso(notNull, contains);

                var lambda = Expression.Lambda<Func<T, bool>>(andExpr, parameter);
                query = query.Where(lambda);
            }

                
        }

        // ----- Sorting -----
        if (!string.IsNullOrWhiteSpace(tableRequest.SortColumn))
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = typeof(T).GetProperty(tableRequest.SortColumn);

            if (property != null)
            {
                var propertyAccess = Expression.Property(parameter, property);
                var orderByExp = Expression.Lambda(propertyAccess, parameter);

                var methodName = tableRequest.SortDirection?.ToLower() == "desc"
                    ? "OrderByDescending"
                    : "OrderBy";

                var resultExp = Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new[] { typeof(T), property.PropertyType },
                    query.Expression,
                    Expression.Quote(orderByExp));

                query = query.Provider.CreateQuery<T>(resultExp);
            }
        }

        // ----- Pagination -----
        var skip = (tableRequest.PageNumber - 1) * tableRequest.PageSize;
        query = query.Skip(skip).Take(tableRequest.PageSize);

        var items = query.ToList();

        return new TableResult<T>
        {
            Items = items,
            TotalCount = totalResults // still pre-search / pre-paging count
        };
    }

    public static TableRequest GetTableRequest(this HttpRequest request)
    {
        return new TableRequest()
        {
            PageNumber = request.Query.ContainsKey("pageNumber") ? int.Parse(request.Query["pageNumber"]) : 1,
            PageSize = request.Query.ContainsKey("pageSize") ? int.Parse(request.Query["pageSize"]) : 10,
            Search = request.Query.ContainsKey("search") ? request.Query["search"].ToString() : null,
            SortColumn = request.Query.ContainsKey("sortColumn") ? request.Query["sortColumn"].ToString() : null,
            SortColumnIndex = request.Query.ContainsKey("sortColumnIndex")
                ? int.Parse(request.Query["sortColumnIndex"])
                : null,
            SortDirection = request.Query.ContainsKey("sortDirection")
                ? request.Query["sortDirection"].ToString()
                : null,
        };
    }
}