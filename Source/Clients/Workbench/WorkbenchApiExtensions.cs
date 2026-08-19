// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.XPath;
using Cratis.Arc.Swagger;
using Cratis.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Cratis.Chronicle.Workbench;

/// <summary>
/// Extension methods for hosting the Workbench's HTTP surface.
/// </summary>
/// <remarks>
/// The Workbench is served by the kernel itself, so there is no separate API application to configure - the
/// commands and read models it calls are the kernel's own Arc artifacts.
/// </remarks>
public static class WorkbenchApiExtensions
{
    /// <summary>
    /// Adds the services the Workbench's HTTP surface needs.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddChronicleWorkbenchApi(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

        services.TryAddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();
        services.AddHttpContextAccessor();
        services.AddSwaggerGen(options =>
        {
            options.IncludeXmlComments(ReadXmlComments);
            options.AddConcepts();
        });

        return services;
    }

    /// <summary>
    /// Configures the request pipeline for the Workbench's HTTP surface.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseChronicleWorkbenchApi(this IApplicationBuilder app)
    {
        if (RuntimeEnvironment.IsDevelopment)
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCratisArc();
        app.UseRouting();
        app.UseWebSockets();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
        app.UseSwagger();
        app.UseSwaggerUI(ConfigureSwaggerUI);

        return app;
    }

    /// <summary>
    /// Applies the dark theme the Workbench's Swagger UI is shown with.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerUIOptions"/> to configure.</param>
    public static void ConfigureSwaggerUI(SwaggerUIOptions options)
    {
        var resourceName = $"{typeof(WorkbenchApiExtensions).Namespace}.SwaggerDark.css";
        using var stream = typeof(WorkbenchApiExtensions).Assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            return;
        }

        using var streamReader = new StreamReader(stream);
        options.HeadContent = $"{options.HeadContent}<style>{streamReader.ReadToEnd()}</style>";
    }

    static XPathDocument ReadXmlComments()
    {
        var type = typeof(WorkbenchApiExtensions);
        var resourceName = $"{type.Assembly.GetName().Name}.xml";
        using var stream = type.Assembly.GetManifestResourceStream(resourceName);
        using var reader = XmlReader.Create(stream!);
        return new XPathDocument(reader);
    }
}
