using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using SimpleTable.Models;

namespace SimpleTable;

public static class Extensions
{
    public static TableResult<T> ToTableResponse<T>(this IQueryable<T> query, TableRequest tableRequest = null, int maxObjectDepthsearch = 3)
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

            var visitedTypes = new HashSet<Type>();
            var body = BuildSearchExpressionIEnumerable(typeof(T), parameter, 0, visitedTypes, maxObjectDepthsearch, searchLower);

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
            TotalCount = totalResults // pre-filter count; change if you want post-search count instead
        };
    }

    public static TableResult<T> ToTableResponse<T>(this IEnumerable<T> source, TableRequest tableRequest = null, int maxObjectDepthsearch = 3)
    {
        // materialize immediately (IEnumerable is pull-based)
        var query = source;

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

        // ========= SEARCH (still uses Expression but runs in-memory) =========
        if (!string.IsNullOrWhiteSpace(tableRequest.Search))
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var searchLower = tableRequest.Search.ToLower();

            var visitedTypes = new HashSet<Type>();
            var body = BuildSearchExpressionIEnumerable(
                typeof(T),
                parameter,
                0,
                visitedTypes,
                maxObjectDepthsearch,
                searchLower);

            if (body != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
                query = query.Where(lambda.Compile()); // Enumerable version
            }
        }

        // ========= SORTING (LINQ-to-Objects) =========
        if (!string.IsNullOrWhiteSpace(tableRequest.SortColumn))
        {
            var prop = typeof(T).GetProperty(
                tableRequest.SortColumn,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop != null)
            {
                Func<T, object> keySelector = x => prop.GetValue(x);

                query = (tableRequest.SortDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true)
                    ? query.OrderByDescending(keySelector)
                    : query.OrderBy(keySelector);
            }
        }

        // ========= PAGINATION (LINQ-to-Objects) =========
        var skip = (tableRequest.PageNumber - 1) * tableRequest.PageSize;
        query = query.Skip(skip).Take(tableRequest.PageSize);

        return new TableResult<T>
        {
            Items = query.ToList(),
            TotalCount = totalResults
        };
    }


    private static bool IsSimple(Type t)
    {
        return t.IsPrimitive ||
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
    }

    private static bool HasNoSearch(MemberInfo mi) => Attribute.IsDefined(mi, typeof(NoSearchAttribute));

    private static bool TypeHasNoSearch(Type type) => Attribute.IsDefined(type, typeof(NoSearchAttribute));

    
    /// <summary>
    /// This builds a search expression base for entity framework
    /// It skips properties marked with [NoSearch]
    /// It has a max depth to avoid too deep recursion
    /// It handles string properties, simple types (int, DateTime, etc) and navigation properties
    /// All objects that can be cast to a string are searched via ToString().ToLower().Contains(...)
    /// </summary>
    /// <param name="currentType"></param>
    /// <param name="instance"></param>
    /// <param name="depth"></param>
    /// <param name="visited"></param>
    /// <param name="maxObjectDepthsearch"></param>
    /// <param name="searchLower"></param>
    /// <returns></returns>
    private static Expression? BuildSearchExpression(Type currentType, Expression? instance, int depth, HashSet<Type> visited, int maxObjectDepthsearch, string searchLower)
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
        {
            return null;
        }

        // avoid type cycles (e.g. Car -> CarMaker -> Cars -> Car ...)
        if (!visited.Add(currentType))
        {
            return null;
        }

        Expression? orExpr = null;
        var properties = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (prop.GetIndexParameters().Length > 0)
            {
                continue;
            }
            if (HasNoSearch(prop))
            {
                continue;
            }
            var propType = prop.PropertyType;
            var propExpr = Expression.Property(instance, prop);
            var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes);
            var searchLowerConst = Expression.Constant(searchLower);
            var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
            if (propType == typeof(string))
            {
                var notNull = Expression.NotEqual(propExpr, Expression.Constant(null, typeof(string)));
                var toLowerCall = Expression.Call(propExpr, toLowerMethod!);
                var containsCall = Expression.Call(toLowerCall, containsMethod!, searchLowerConst);
                var andExpr = Expression.AndAlso(notNull, containsCall);
                orExpr = orExpr == null ? andExpr : Expression.OrElse(orExpr, andExpr);
            }
            else if (IsSimple(propType))
            {
                var toString = Expression.Call(propExpr, nameof(object.ToString), Type.EmptyTypes);
                if (toString != null)
                {
                    var notNull = Expression.NotEqual(toString, Expression.Constant(null, typeof(string)));
                    var toLower = Expression.Call(toString, toLowerMethod!);
                    var contains = Expression.Call(toLower, containsMethod!, searchLowerConst);
                    var andExpr = Expression.AndAlso(notNull, contains);

                    orExpr = orExpr == null ? andExpr : Expression.OrElse(orExpr, andExpr);
                }
            }
            else if (!IsSimple(propType) && !typeof(System.Collections.IEnumerable).IsAssignableFrom(propType))
            {
                var nested = BuildSearchExpression(propType, propExpr, depth + 1, visited, maxObjectDepthsearch, searchLower);
                if (nested != null)
                {
                    orExpr = orExpr == null ? nested : Expression.OrElse(orExpr, nested);
                }
            }
        }

        visited.Remove(currentType);
        return orExpr;
    }

    /// <summary>
    /// This builds a search expression base on an IEnumerable type
    /// It skips properties marked with [NoSearch]
    /// It has a max depth to avoid too deep recursion
    /// It handles string properties, simple types (int, DateTime, etc) and navigation properties
    /// All objects that can be cast to a string are searched via ToString().ToLower().Contains(...)
    /// </summary>
    /// <param name="currentType"></param>
    /// <param name="instance"></param>
    /// <param name="depth"></param>
    /// <param name="visited"></param>
    /// <param name="maxObjectDepthsearch"></param>
    /// <param name="searchLower"></param>
    /// <returns></returns>
    private static Expression? BuildSearchExpressionIEnumerable(Type currentType, Expression? instance, int depth, HashSet<Type> visited, int maxObjectDepthsearch, string searchLower)
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
        {
            return null;
        }

        // avoid type cycles (e.g. Car -> CarMaker -> Cars -> Car ...)
        if (!visited.Add(currentType))
        {
            return null;
        }

        Expression? orExpr = null;
        var properties = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
        var searchLowerConst = Expression.Constant(searchLower);

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
                var notNull = Expression.NotEqual(propExpr, Expression.Constant(null, typeof(string)));
                var toLowerCall = Expression.Call(propExpr, toLowerMethod);
                var containsCall = Expression.Call(toLowerCall, containsMethod, searchLowerConst);
                var andExpr = Expression.AndAlso(notNull, containsCall);
                orExpr = orExpr == null ? andExpr : Expression.OrElse(orExpr, andExpr);
            }
            // For simple types (int, DateTime, etc) use ToString().ToLower().Contains(...)
            else if (IsSimple(propType))
            {
                var toStringMethod = propType.GetMethod(nameof(object.ToString), Type.EmptyTypes);
                if (toStringMethod != null)
                {
                    // propExpr.ToString()
                    var toStringCall = Expression.Call(propExpr, toStringMethod);

                    var notNull = Expression.NotEqual(
                        toStringCall,
                        Expression.Constant(null, typeof(string)));

                    var toLower = Expression.Call(toStringCall, toLowerMethod);
                    var contains = Expression.Call(toLower, containsMethod, searchLowerConst);

                    var andExpr = Expression.AndAlso(notNull, contains);
                    orExpr = orExpr == null ? andExpr : Expression.OrElse(orExpr, andExpr);
                }
            }
            // ICollection / IEnumerable navigation properties
            else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) && propType != typeof(string))
            {
                // Figure out the element type, e.g. ICollection<Car> -> Car
                Type? elementType = null;

                if (propType.IsArray)
                {
                    elementType = propType.GetElementType();
                }
                else if (propType.IsGenericType)
                {
                    elementType = propType.GetGenericArguments().FirstOrDefault();
                }

                if (elementType == null)
                {
                    continue;
                }

                // Respect [NoSearch] on the element type
                if (TypeHasNoSearch(elementType))
                {
                    continue;
                }

                var elementParam = Expression.Parameter(elementType, "e");

                Expression? elementBody = null;

                // Collection of string: e != null && e.ToLower().Contains(searchLower)
                if (elementType == typeof(string))
                {
                    var notNull = Expression.NotEqual(elementParam, Expression.Constant(null, typeof(string)));
                    var elemToLower = Expression.Call(elementParam, toLowerMethod);
                    var elemContains = Expression.Call(elemToLower, containsMethod, searchLowerConst);
                    elementBody = Expression.AndAlso(notNull, elemContains);
                }
                // Collection of simple types: e.ToString().ToLower().Contains(searchLower)
                else if (IsSimple(elementType))
                {
                    var toStringMethod = elementType.GetMethod(nameof(object.ToString), Type.EmptyTypes);
                    if (toStringMethod == null)
                    {
                        continue;
                    }

                    var toStringCall = Expression.Call(elementParam, toStringMethod);
                    var notNull = Expression.NotEqual(toStringCall, Expression.Constant(null, typeof(string)));
                    var elemToLower = Expression.Call(toStringCall, toLowerMethod);
                    var elemContains = Expression.Call(elemToLower, containsMethod, searchLowerConst);

                    elementBody = Expression.AndAlso(notNull, elemContains);
                }
                // Collection of complex types: recurse
                else
                {
                    elementBody = BuildSearchExpressionIEnumerable(elementType, elementParam, depth + 1, visited, maxObjectDepthsearch, searchLower);
                }

                if (elementBody != null)
                {
                    var anyLambda = Expression.Lambda(elementBody, elementParam);

                    var anyMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .First(m =>
                        {
                            if (m.Name != "Any") return false;
                            var args = m.GetParameters();
                            return args.Length == 2;
                        })
                        .MakeGenericMethod(elementType);
                    var anyCall = Expression.Call(anyMethod, propExpr, anyLambda);
                    orExpr = orExpr == null ? anyCall : Expression.OrElse(orExpr, anyCall);
                }
            }
            // Recurse into reference-type navigation properties (e.g. CarMaker),
            // unless they are simple types or collections
            else if (!IsSimple(propType))
            {
                var nested = BuildSearchExpressionIEnumerable(propType, propExpr, depth + 1, visited, maxObjectDepthsearch, searchLower);
                if (nested != null)
                {
                    orExpr = orExpr == null ? nested : Expression.OrElse(orExpr, nested);
                }
            }
        }

        visited.Remove(currentType);
        return orExpr;
    }


    /// <summary>
    /// Returns a TableRequest object from the HttpRequest query parameters, if parameters are not present, default values are used.
    /// PageSize: 10
    /// PageNumber: 1
    /// SortDirection: null
    /// SortColumn: null
    /// SortColumnIndex: null
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static TableRequest GetTableRequest(this HttpRequest request)
    {
        return new TableRequest()
        {
            PageNumber = request.Query.ContainsKey("pageNumber") ? int.Parse(request.Query["pageNumber"]) : 1,
            PageSize = request.Query.ContainsKey("pageSize") ? int.Parse(request.Query["pageSize"]) : 10,
            Search = request.Query.ContainsKey("search") ? request.Query["search"].ToString() : null,
            SortColumn = request.Query.ContainsKey("sortColumn") ? request.Query["sortColumn"].ToString() : null,
            SortColumnIndex = request.Query.ContainsKey("sortColumnIndex") ? int.Parse(request.Query["sortColumnIndex"]) : null,
            SortDirection = request.Query.ContainsKey("sortDirection") ? request.Query["sortDirection"].ToString() : null,
        };
    }
}