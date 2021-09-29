// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Collections;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Extensions.GraphQL
{
    public static class GraphQLBuilderExtensions
    {
        record MethodAtPath(string Path, string Name, MethodInfo Method);

        public static IRequestExecutorBuilder AddQueries(this IRequestExecutorBuilder builder, IGraphControllers graphControllers, INamingConventions namingConventions)
        {
            return builder.AddQueries(graphControllers, namingConventions, out _);
        }

        public static IRequestExecutorBuilder AddQueries(this IRequestExecutorBuilder builder, IGraphControllers graphControllers, INamingConventions namingConventions, out SchemaRoute query)
        {
            query = BuildSchemaRoutesWithItems<QueryAttribute>("Query", graphControllers, namingConventions, "Queries");
            builder.AddQueryType(query);
            return builder;
        }

        public static IRequestExecutorBuilder AddMutations(this IRequestExecutorBuilder builder, IGraphControllers graphControllers, INamingConventions namingConventions)
        {
            return builder.AddMutations(graphControllers, namingConventions, out _);
        }

        public static IRequestExecutorBuilder AddMutations(this IRequestExecutorBuilder builder, IGraphControllers graphControllers, INamingConventions namingConventions, out SchemaRoute mutation)
        {
            mutation = BuildSchemaRoutesWithItems<MutationAttribute>("Mutation", graphControllers, namingConventions, "Mutations");
            builder.AddMutationType(mutation);
            return builder;
        }

        public static IRequestExecutorBuilder AddSubscriptions(this IRequestExecutorBuilder builder, IGraphControllers graphControllers, INamingConventions namingConventions, out SchemaRoute subscription)
        {
            subscription = BuildSchemaRoutesWithItems<SubscriptionAttribute>("Subscription", graphControllers, namingConventions, "Subscriptions");
            builder.AddSubscriptionType(subscription);
            return builder;
        }

        static SchemaRoute BuildSchemaRoutesWithItems<TAttribute>(string rootName, IGraphControllers graphControllers, INamingConventions namingConventions, string postFix)
            where TAttribute : Attribute, ICanHavePath
        {
            var methods = GetMethodsAdornedWithAttribute<TAttribute>(graphControllers, namingConventions);
            var root = new SchemaRoute(string.Empty, rootName, rootName);
            var routesByPath = BuildRouteHierarchy(root, methods, postFix);

            var topLevelRoutes = routesByPath.Where((keyValue) => !keyValue.Key.Contains('/') && keyValue.Key.Length > 0).Select((keyValue) => keyValue.Value);
            topLevelRoutes.ForEach(root.AddChild);
            methods.ForEach(_ => routesByPath[_.Path].AddItem(new SchemaRouteItem(_.Method, _.Name)));

            return root;
        }

        static IOrderedEnumerable<MethodAtPath> GetMethodsAdornedWithAttribute<TAttribute>(IGraphControllers graphControllers, INamingConventions namingConventions) where TAttribute : Attribute, ICanHavePath
        {
            return graphControllers.All
                .SelectMany(_ => _.GetMethodsWithAttribute<TAttribute>())
                .Select(_ =>
                {
                    var rootPath = _.DeclaringType!.GetRootPath();
                    var hasPath = _.HasPath<TAttribute>();
                    var localPath = hasPath ? _.GetPath<TAttribute>() : namingConventions.GetMemberName(_, MemberKind.Field).Value;
                    var path = $"{rootPath}{(rootPath.Length > 0 ? "/" : "")}{localPath}";
                    var lastSlash = path.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase);
                    var name = path;

                    if (lastSlash >= 0)
                    {
                        name = path[(lastSlash + 1)..];
                        path = path[..lastSlash];
                    }
                    else
                    {
                        path = string.Empty;
                    }

                    return new MethodAtPath(path, name, _);
                })
                .OrderBy(_ => _.Path);
        }

        static IDictionary<string, SchemaRoute> BuildRouteHierarchy(SchemaRoute root, IEnumerable<MethodAtPath> methods, string postFix)
        {
            var distinctPaths = methods.GroupBy(_ => _.Path).Select(_ => _.First()).Select(_ => _.Path);
            var routesByPath = new Dictionary<string, SchemaRoute>
            {
                { string.Empty, root }
            };

            foreach (var path in distinctPaths)
            {
                var current = string.Empty;
                var segments = path.Split('/');
                SchemaRoute currentRoute = null!;
                SchemaRoute parentRoute = null!;
                foreach (var segment in segments)
                {
                    current = $"{current}{(current.Length > 0 ? "/" : "")}{segment}";
                    if (routesByPath.ContainsKey(current))
                    {
                        currentRoute = routesByPath[current];
                    }
                    else
                    {
                        currentRoute = new SchemaRoute(current, segment, $"_{segment}{postFix}");
                        routesByPath[current] = currentRoute;
                        parentRoute?.AddChild(currentRoute);
                    }
                    parentRoute = currentRoute;
                }
            }

            return routesByPath;
        }
    }
}
