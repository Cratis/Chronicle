// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using Autofac;
using Cratis.Events;
using Cratis.Events.Observation.Grpc;
using Cratis.Events.Store;
using Cratis.Events.Store.Grpc;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Streams;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;

namespace Cratis.Server
{
    public class Startup
    {
        internal static readonly ITypes Types = new Types.Types();
        internal static ILifetimeScope? AutofacContainer;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Types);
            services.AddMvc();

            foreach (var controllerAssembly in Types.FindMultiple<Controller>().Select(_ => _.Assembly).Distinct())
            {
                services.AddControllers().PartManager.ApplicationParts.Add(new AssemblyPart(controllerAssembly));
            }

            // services.AddCodeFirstGrpc(config => config.ResponseCompressionLevel = CompressionLevel.Optimal);
            // services.AddCodeFirstGrpcReflection();
            // services.AddEndpointsApiExplorer();
            // services.AddSwaggerGen();
            //services.TryAddSingleton(BinderConfiguration.Create(binder: new ServiceBinderWithServiceResolutionFromServiceCollection(services)));
        }

        public void ConfigureContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder
                .RegisterDefaults(Types)
                .RegisterBuildCallback(_ => AutofacContainer = _);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExecutionContext();

            ContainerBuilderExtensions.ServiceProvider = app.ApplicationServices;
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseSwagger();
            // app.UseSwaggerUI();

            app.PerformBootProcedures();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                // endpoints.MapGrpcService<EventLogService>();
                // endpoints.MapGrpcService<ObserversService>();
                // endpoints.MapCodeFirstGrpcReflectionService();
            });

            var streamProvider = app.ApplicationServices.GetService<GetClusterClient>()!().GetStreamProvider("event-log");
            var stream = streamProvider.GetStream<AppendedEvent>(Guid.Empty, "greetings");
            stream.SubscribeAsync(
                (@event, st) =>
                {
                    Console.WriteLine("Event received");
                    return Task.CompletedTask;
                }); //, new EventSequenceToken(0));
        }
    }

    [Route("/api/test")]
    public class TestController : Controller
    {
        readonly IEventStore _eventStore;
        readonly IExecutionContextManager _executionContextManager;

        public TestController(IEventStore eventStore, IExecutionContextManager executionContextManager)
        {
            _eventStore = eventStore;
            _executionContextManager = executionContextManager;
        }

        [HttpGet]
        public async Task DoStuff()
        {
            _executionContextManager.Establish(
                Guid.Parse("f455c031-630e-450d-a75b-ca050c441708"),
                Guid.NewGuid().ToString()
            );

            await _eventStore.GetEventLog(Guid.Empty).Append(
                Guid.NewGuid().ToString(),
                new EventType(Guid.NewGuid(), 1),
                "{ \"blah\": 42 }"
            );
        }
    }
}
