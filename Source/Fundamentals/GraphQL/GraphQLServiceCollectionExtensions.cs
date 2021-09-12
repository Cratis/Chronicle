// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Cratis.Types;
using Cratis.GraphQL;
using Cratis.GraphQL.Concepts;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Cratis.Collections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> for adding GraphQL services.
    /// </summary>
    public static class GraphQLServiceCollectionExtensions
    {
        public const char UuidFormat = 'D';

        public static void AddGraphQL(this IServiceCollection services, IWebHostEnvironment environment, ITypes types)
        {
            var graphControllers = new GraphControllers(types);
            services.Add(new ServiceDescriptor(typeof(IGraphControllers), graphControllers));

            services.AddSingleton<ITypeInspector, TypeInspector>();

            foreach (var graphControllerType in graphControllers.All)
            {
                services.AddTransient(graphControllerType);
            }

            var graphQLBuilder = services
                                    .AddGraphQLServer()
                                    .AddInMemorySubscriptions()
                                    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = environment.IsDevelopment())
                                    .AddAuthorization()
                                    .AddType(new UuidType(UuidFormat));
            types.FindMultiple<ScalarType>().Where(_ => !_.IsGenericType).ForEach(_ => graphQLBuilder.AddType(_));

            var namingConventions = new NamingConventions();
            services.AddSingleton<INamingConventions>(namingConventions);

            graphQLBuilder
                .AddQueries(graphControllers, namingConventions, out var queries)
                .AddMutations(graphControllers, namingConventions, out var mutations)
                .AddSubscriptions(graphControllers, namingConventions, out var subscriptions);

            var systemQueries = new SchemaRoute("system", "system", "_system");
            queries.AddChild(systemQueries);

            types.FindMultiple(typeof(ConceptAs<>)).ForEach(_ => graphQLBuilder.AddConceptTypeConverter(_));

            if (environment.IsDevelopment())
            {
                graphQLBuilder.AddApolloTracing();
            }
        }
    }
}
