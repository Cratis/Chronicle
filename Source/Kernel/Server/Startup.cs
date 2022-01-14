// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Autofac;
using Cratis.Concepts.SystemJson;
using Cratis.Events.Projections;
using Cratis.Hosting;
using Cratis.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Cratis.Server
{
    public class Startup
    {
        internal static readonly ITypes Types = new Types.Types();
        internal static ILifetimeScope? AutofacContainer;

        IServiceCollection _services = new ServiceCollection();

        public void ConfigureServices(IServiceCollection services)
        {
            _services = services;
            Types.RegisterTypeConvertersForConcepts();

            services
                .AddSingleton(Types)
                .AddConfigurationObjects(Types);
            services.AddMvc();

            var controllerBuilder = services
                .AddControllers(_ => _.AddCQRS())
                .AddJsonOptions(_ => _.JsonSerializerOptions.Converters.Add(new ConceptAsJsonConverterFactory()));

            foreach (var controllerAssembly in Types.FindMultiple<Controller>().Select(_ => _.Assembly).Distinct())
            {
                controllerBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(controllerAssembly));
            }

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }

        public void ConfigureContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder
                .RegisterDefaults(Types, _services)
                .RegisterBuildCallback(_ => AutofacContainer = _);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExecutionContext();
            MongoDBReadModels.ConfigureReadModels(app.ApplicationServices).Wait();

            ContainerBuilderExtensions.ServiceProvider = app.ApplicationServices;
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.PerformBootProcedures();

            app.UseCratis();

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());

            app.RunAsSinglePageApplication();

            // TODO: This needs to be improved.
            // In a regular client, this is hooked up with a hosted service, that is too early within the kernel
            app.ApplicationServices.GetService<IProjectionsRegistrar>()!.StartAll().Wait();
        }
    }
}
