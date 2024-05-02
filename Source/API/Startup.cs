// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.API.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });
        services.AddSwaggerGen(options =>
        {
            options.SchemaFilter<EnumSchemaFilter>();
            var filePath = Path.Combine(AppContext.BaseDirectory, typeof(Startup).Assembly.GetName().Name + ".xml");
            options.IncludeXmlComments(filePath);
            options.OperationFilter<CommandResultOperationFilter>();
            options.OperationFilter<QueryResultOperationFilter>();
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseStaticFiles();
        app.UseRouting();
        app.UseWebSockets();
        app.UseSwagger();
        app.UseSwaggerUI(options => options.InjectStylesheet("/swagger-ui/SwaggerDark.css"));
    }
}
