// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using Autofac;
using Cratis.DependencyInversion;
using Cratis.Events.Observation.Grpc;
using Cratis.Events.Store.Grpc;
using Cratis.GraphQL;
using Cratis.Types;
using GraphQL.Server.Ui.Playground;
using HotChocolate.AspNetCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;

namespace Cratis.Server
{
    public class Startup
    {
        internal static readonly ITypes Types = new Types.Types();
        internal static ILifetimeScope? AutofacContainer;
        readonly IWebHostEnvironment _environment;

        public Startup(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Types);
            services.AddGraphQL(_environment, Types);

            services.AddCodeFirstGrpc(config => config.ResponseCompressionLevel = CompressionLevel.Optimal);
            services.AddCodeFirstGrpcReflection();
            services.TryAddSingleton(BinderConfiguration.Create(binder: new ServiceBinderWithServiceResolutionFromServiceCollection(services)));
        }

        public void ConfigureContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterDefaults(Types);
            containerBuilder.RegisterBuildCallback(_ => AutofacContainer = _);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExecutionContext();

            SchemaRoute.ServiceProvider = app.ApplicationServices;
            ContainerBuilderExtensions.ServiceProvider = app.ApplicationServices;
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                if (env.IsDevelopment())
                {
                    endpoints.MapGraphQLPlayground(
                        new PlaygroundOptions
                        {
                            GraphQLEndPoint = "/graphql",
                            SubscriptionsEndPoint = "/graphql"
                        }, "/graphql/ui");
                }

                endpoints
                    .MapGraphQL("/graphql")
                    .WithOptions(new GraphQLServerOptions()
                    {
                        EnableSchemaRequests = _environment.IsDevelopment(),
                        Tool = { Enable = false }
                    });

                endpoints.MapGrpcService<EventLogService>();
                endpoints.MapGrpcService<ObserversService>();
                endpoints.MapCodeFirstGrpcReflectionService();
            });
        }
    }
}
