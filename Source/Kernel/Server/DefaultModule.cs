// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Autofac;
using Cratis.Events.Store;
using Cratis.Execution;
using Cratis.Extensions.Orleans.Execution;
using Orleans;
using Orleans.Configuration;

namespace Cratis.Server
{
    public class DefaultModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register<GetClusterClient>(_ =>
            {
                var clientBuilder = new ClientBuilder()
                                            .UseLocalhostClustering()
                                            //.UseServiceProviderFactory(new ClientServiceProviderFactory())
                                            .AddEventLogStream()
                                            .ConfigureServices(services =>
                                            {
                                                services.AddSingleton<IExecutionContextManager, ExecutionContextManager>();
                                                services.AddSingleton<IRequestContextManager, RequestContextManager>();
                                            })
                                            .UseExecutionContext()
                                            .Configure<ClusterOptions>(options =>
                                            {
                                                options.ClusterId = "dev";
                                                options.ServiceId = "Cratis Kernel";
                                            });

                var rr = _.ComponentRegistry.Registrations.Where(r => r.Services.Any(s => s.Description.Contains("ExecutionContextManager"))).ToArray();
                var client = clientBuilder.Build();
                client.Connect().Wait();
                return () => client;
            }).As<GetClusterClient>().SingleInstance();
        }
    }
}
