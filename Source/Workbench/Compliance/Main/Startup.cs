// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Autofac;
using Cratis.Concepts.SystemJson;
using Cratis.Hosting;
using Cratis.Types;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Cratis.Compliance.Main
{
    public class Startup
    {
        internal static readonly ITypes Types = new Types.Types();
        internal static ILifetimeScope? AutofacContainer;

        IServiceCollection? _services;

        public void ConfigureServices(IServiceCollection services)
        {
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

            _services = services;
        }

        public void ConfigureContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder
                .RegisterDefaults(Types, _services)
                .RegisterBuildCallback(_ => AutofacContainer = _);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            ContainerBuilderExtensions.ServiceProvider = app.ApplicationServices;
            MongoDBReadModels.ConfigureReadModels(app.ApplicationServices).Wait();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseWebSockets();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.PerformBootProcedures();

            app.UseCratis();

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());

            app.RunAsSinglePageApplication();
        }
    }
}
