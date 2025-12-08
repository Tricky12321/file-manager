using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SimpleTable.Models;

namespace SimpleTable;

public static class Extensions
{
    public static TableResult<T> ToTableResponse<T>(this IQueryable<T> query, DbContext db, TableRequest? tableRequest = null, int maxObjectDepthsearch = 3, bool searchOnlyEfMapped = true)
    {
        var totalResults = query.Count();

        if (tableRequest == null)
        {
            tableRequest = new TableRequest
            {
                PageNumber = 1,
                PageSize = totalResults,
                Search = "",
                SortColumn = null,
                SortDirection = null
            };
        }

        // ========== SEARCH ==========
        if (!string.IsNullOrWhiteSpace(tableRequest.Search))
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var search = tableRequest.Search.ToLower();

            var visited = new HashSet<Type>();

            Expression? body;

            if (searchOnlyEfMapped)
            {
                var included = searchOnlyEfMapped ? GetIncludedNavigations(query) : null;
                // Original behavior: scan ALL public CLR properties
                body = BuildEfSearchExpression(db.Model.FindEntityType(typeof(T)), parameter, 0, visited, maxObjectDepthsearch, search, db.Model, included);
                // Only scan EF properties
            }
            else
            {
                body = BuildEfSearchExpression(db.Model.FindEntityType(typeof(T)), parameter, 0, visited, maxObjectDepthsearch, search, db.Model);
            }

            if (body != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
                query = query.Where(lambda);
            }
        }

        if (!string.IsNullOrWhiteSpace(tableRequest.SortColumn))
        {
            var prop = typeof(T).GetProperty(tableRequest.SortColumn, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var methodName = tableRequest.SortDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true ? "OrderByDescending" : "OrderBy";
                var resultExp = Expression.Call(typeof(Queryable), methodName, new[] { typeof(T), prop.PropertyType }, query.Expression,
                    Expression.Quote(Expression.Lambda(Expression.Property(parameter, prop), parameter)));
                query = query.Provider.CreateQuery<T>(resultExp);
            }
        }

        // ========== PAGINATION ==========
        var skip = (tableRequest.PageNumber - 1) * tableRequest.PageSize;
        query = query.Skip(skip).Take(tableRequest.PageSize);

        return new TableResult<T>
        {
            Items = query.ToList(),
            TotalCount = totalResults
        };
    }

    public static TableResult<T> ToTableResponse<T>(this IEnumerable<T> source, TableRequest? tableRequest = null, int maxObjectDepthsearch = 3)
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
            var body = BuildSearchExpressionIEnumerable(typeof(T), parameter, 0, visitedTypes, maxObjectDepthsearch, searchLower);

            if (body != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
                query = query.Where(lambda.Compile()); // Enumerable version
            }
        }

        // ========= SORTING (LINQ-to-Objects) =========
        if (!string.IsNullOrWhiteSpace(tableRequest.SortColumn))
        {
            var prop = typeof(T).GetProperty(tableRequest.SortColumn, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop != null)
            {
                Func<T, object> keySelector = x => prop.GetValue(x);
                query = (tableRequest.SortDirection?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true) ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
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
        return t.IsPrimitive || t.IsEnum || t == typeof(decimal) || t == typeof(float) || t == typeof(int) ||
               t == typeof(long) || t == typeof(ulong) || t == typeof(Int16) || t == typeof(Int32) ||
               t == typeof(Int64) || t == typeof(double) || t == typeof(bool) || t == typeof(DateTime) ||
               t == typeof(DateTimeOffset) || t == typeof(Guid) || t == typeof(TimeSpan);
    }

    private static bool HasNoSearch(MemberInfo mi) => Attribute.IsDefined(mi, typeof(NoSearchAttribute));

    private static bool TypeHasNoSearch(Type type) => Attribute.IsDefined(type, typeof(NoSearchAttribute));


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
                    var toStringCall = Expression.Call(propExpr, toStringMethod);
                    var notNull = Expression.NotEqual(toStringCall, Expression.Constant(null, typeof(string)));
                    var toLower = Expression.Call(toStringCall, toLowerMethod);
                    var contains = Expression.Call(toLower, containsMethod, searchLowerConst);
                    var andExpr = Expression.AndAlso(notNull, contains);
                    orExpr = orExpr == null ? andExpr : Expression.OrElse(orExpr, andExpr);
                }
            }
            else if (typeof(IEnumerable).IsAssignableFrom(propType) && propType != typeof(string))
            {
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


    private static Expression? BuildEfSearchExpression(IEntityType? entityType, Expression parameter, int depth,
        HashSet<Type> visited, int maxDepth, string searchLower, IModel efModel, HashSet<string>? includedNavigations = null)
    {
        if (depth > maxDepth)
        {
            return null;
        }

        if (!visited.Add(entityType.ClrType))
        {
            return null;
        }

        Expression? combined = null;
        // Scalar properties
        foreach (var prop in entityType.GetProperties())
        {
            if (prop.ClrType == typeof(string))
            {
                var toLower = Expression.Call(Expression.Property(parameter, prop.PropertyInfo), typeof(string).GetMethod("ToLower", Type.EmptyTypes));
                var contains = Expression.Call(toLower, typeof(string).GetMethod("Contains", new[] { typeof(string) }), Expression.Constant(searchLower));
                combined = combined == null ? contains : Expression.OrElse(combined, contains);
            }
        }

        // Navigation properties
        // Navigation properties (reference + collections)
        foreach (var nav in entityType.GetNavigations())
        {
            if (includedNavigations != null && !includedNavigations.Contains(nav.Name))
            {
                continue;
            }

            var navEntityType = efModel.FindEntityType(nav.TargetEntityType.ClrType);
            if (navEntityType == null)
            {
                continue;
            }

            // x.Nav
            Expression navAccess = Expression.Property(parameter, nav.PropertyInfo);

            // If navigation is a collection => use Any()
            if (typeof(IEnumerable).IsAssignableFrom(nav.PropertyInfo.PropertyType) && nav.PropertyInfo.PropertyType != typeof(string))
            {
                // Determine element type: for ICollection<T> it's the generic argument
                var itemParam = Expression.Parameter(navEntityType.ClrType, "f");
                // recursive search inside collection item
                var innerSearch = BuildEfSearchExpression(navEntityType, itemParam, depth + 1, visited, maxDepth, searchLower, efModel);
                if (innerSearch != null)
                {
                    var lambda = Expression.Lambda(innerSearch, itemParam);
                    var anyCall = Expression.Call(typeof(Enumerable), "Any", new[] { navEntityType.ClrType }, navAccess, lambda);
                    combined = combined == null ? anyCall : Expression.OrElse(combined, anyCall);
                }
            }
            else
            {
                // normal reference navigation
                var inner = BuildEfSearchExpression(navEntityType, navAccess, depth + 1, visited, maxDepth, searchLower, efModel);
                if (inner != null)
                {
                    var safe = Expression.AndAlso(Expression.NotEqual(navAccess, Expression.Constant(null)), inner);
                    combined = combined == null ? safe : Expression.OrElse(combined, safe);
                }
            }
        }


        return combined;
    }


    public static HashSet<string> GetIncludedNavigations(IQueryable query)
    {
        var visitor = new IncludeVisitor();
        visitor.Visit(query.Expression);
        return visitor.Includes;
    }

    class IncludeVisitor : ExpressionVisitor
    {
        public HashSet<string> Includes { get; } = new();

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Include" || node.Method.Name == "ThenInclude")
            {
                var lambda = (LambdaExpression)((UnaryExpression)node.Arguments[1]).Operand;

                // Extract property path "Features", "Owner.Address", etc.
                string path = ExtractPropertyPath(lambda.Body);
                Includes.Add(path);
            }

            return base.VisitMethodCall(node);
        }

        private static string ExtractPropertyPath(Expression exp)
        {
            var parts = new List<string>();

            while (exp is MemberExpression memberExp)
            {
                parts.Add(memberExp.Member.Name);
                exp = memberExp.Expression;
            }

            parts.Reverse();
            return string.Join(".", parts);
        }
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