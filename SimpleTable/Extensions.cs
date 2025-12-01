using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using SimpleTable.Models;

namespace SimpleTable;

public static class Extensions
{
    public static TableResult<T> ToTableResponseDeep<T>(this IQueryable<T> query, TableRequest tableRequest = null, int maxObjectDepthsearch = 3)
    {
        // Count BEFORE filtering/paging (same behavior as your original)
        var totalResults = query.Count();

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

        // ========= SEARCH (recursive, string-only, EF-translatable, [NoSearch]-aware) =========
        if (!string.IsNullOrWhiteSpace(tableRequest.Search))
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var searchLower = tableRequest.Search.ToLower();
            var searchLowerConst = Expression.Constant(searchLower);

            var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes);
            var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });

            bool IsSimple(Type t) =>
                t.IsPrimitive ||
                t.IsEnum ||
                t == typeof(decimal) ||
                t == typeof(float) ||
                t == typeof(int) ||
                t == typeof(long) ||
                t == typeof(ulong) ||
                t == typeof(Int16) ||
                t == typeof(Int32) ||
                t == typeof(Int64) ||
                t == typeof(double) ||
                t == typeof(bool) ||
                t == typeof(DateTime) ||
                t == typeof(DateTimeOffset) ||
                t == typeof(Guid) ||
                t == typeof(TimeSpan);

            bool HasNoSearch(MemberInfo mi) =>
                Attribute.IsDefined(mi, typeof(NoSearchAttribute));

            bool TypeHasNoSearch(Type type) =>
                Attribute.IsDefined(type, typeof(NoSearchAttribute));

            Expression? BuildSearchExpression(Type currentType, Expression instance, int depth, HashSet<Type> visited)
            {
                if (depth > maxObjectDepthsearch)
                {
                    return null;
                }

                if (instance == null)
                {
                    return null;
                }

                // If the type itself is marked [NoSearch], bail out
                if (TypeHasNoSearch(currentType))
                    return null;

                // avoid type cycles (e.g. Car -> CarMaker -> Cars -> Car ...)
                if (!visited.Add(currentType))
                {
                    return null;
                }

                Expression? orExpr = null;
                var properties = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    // skip indexers
                    if (prop.GetIndexParameters().Length > 0)
                    {
                        continue;
                    }

                    // skip properties marked with [NoSearch]
                    if (HasNoSearch(prop))
                    {
                        continue;
                    }

                    var propType = prop.PropertyType;
                    var propExpr = Expression.Property(instance, prop);

                    // Direct string property: x.Prop != null && x.Prop.ToLower().Contains(searchLower)
                    if (propType == typeof(string))
                    {
                        var notNull = Expression.NotEqual(
                            propExpr,
                            Expression.Constant(null, typeof(string)));

                        var toLowerCall = Expression.Call(propExpr, toLowerMethod!);

                        var containsCall = Expression.Call(
                            toLowerCall,
                            containsMethod!,
                            searchLowerConst);

                        var andExpr = Expression.AndAlso(notNull, containsCall);

                        orExpr = orExpr == null ? andExpr : Expression.OrElse(orExpr, andExpr);
                    } else if (IsSimple(propType))
                    {
                        var notNull = Expression.NotEqual(
                            propExpr,
                            Expression.Constant(null, typeof(string)));

                        var toLowerCall = Expression.Call(propExpr, toLowerMethod!);

                        var containsCall = Expression.Call(
                            toLowerCall,
                            containsMethod!,
                            searchLowerConst);

                        var andExpr = Expression.AndAlso(notNull, containsCall);

                        orExpr = orExpr == null ? andExpr : Expression.OrElse(orExpr, andExpr);
                    }
                    // Recurse into reference-type navigation properties (e.g. CarMaker),
                    // unless they are simple types or collections
                    else if (!IsSimple(propType) && !typeof(System.Collections.IEnumerable).IsAssignableFrom(propType))
                    {
                        var nested = BuildSearchExpression(propType, propExpr, depth + 1, visited);
                        if (nested != null)
                        {
                            orExpr = orExpr == null ? nested : Expression.OrElse(orExpr, nested);
                        }
                    }
                    // NOTE: we deliberately skip IEnumerable navigation collections here to keep
                    // the generated expression EF-friendly and avoid complex Any() recursion.
                }

                visited.Remove(currentType);
                return orExpr;
            }

            var visitedTypes = new HashSet<Type>();
            var body = BuildSearchExpression(typeof(T), parameter, 0, visitedTypes);

            if (body != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
                query = query.Where(lambda); // stays IQueryable => executed in DB
            }
        }

        // ========= SORTING (EF-side) =========
        if (!string.IsNullOrWhiteSpace(tableRequest.SortColumn))
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var prop = typeof(T).GetProperty(
                tableRequest.SortColumn,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop != null)
            {
                var propertyAccess = Expression.Property(parameter, prop);
                var orderByExp = Expression.Lambda(propertyAccess, parameter);

                var methodName = tableRequest.SortDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true
                    ? "OrderByDescending"
                    : "OrderBy";

                var resultExp = Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new[] { typeof(T), prop.PropertyType },
                    query.Expression,
                    Expression.Quote(orderByExp));

                query = query.Provider.CreateQuery<T>(resultExp);
            }
        }

        // ========= PAGINATION (EF-side) =========
        var skip = (tableRequest.PageNumber - 1) * tableRequest.PageSize;
        query = query.Skip(skip).Take(tableRequest.PageSize);

        // Materialize once, at the end
        var items = query.ToList();

        return new TableResult<T>
        {
            Items = items,
            TotalCount = totalResults   // pre-filter count; change if you want post-search count instead
        };
    }
    
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