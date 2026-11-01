// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ExternalServices;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Orleans.Hosting;

/// <summary>
/// Extension methods for configuring the <see cref="IExternalServiceHttpClientFactory"/> <see cref="HttpClient"/>.
/// </summary>
public static class ExternalServiceHttpClientExtensions
{
    /// <summary>
    /// Adds the <see cref="HttpClient"/> used for calling external services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ISiloBuilder"/>.</param>
    /// <returns>The builder for continuation.</returns>
    public static ISiloBuilder AddExternalServiceHttpClient(this ISiloBuilder builder)
    {
        builder.Services.TryAddSingleton<IWebhookHostResolver, WebhookHostResolver>();
        builder.Services.TryAddSingleton<IExternalServiceTargetPolicy, ExternalServiceTargetPolicy>();
        builder.Services.AddKeyedSingleton<IOAuthClient>(ExternalServiceHttpClientFactory.HttpClientName, (services, _) => new OAuthClient(
            services.GetRequiredService<IHttpClientFactory>(),
            services.GetRequiredService<IExternalServiceTargetPolicy>(),
            services.GetService<TimeProvider>(),
            ExternalServiceHttpClientFactory.HttpClientName));
        builder.Services.AddHttpClient(ExternalServiceHttpClientFactory.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(services => WebhookHttpClientExtensions.CreateHandler(new WebhookConnection(
                services.GetRequiredService<IWebhookHostResolver>(),
                services.GetRequiredService<IExternalServiceTargetPolicy>())))
            .AddHttpMessageHandler(services => new WebhookValidationHandler(
                services.GetRequiredService<IExternalServiceTargetPolicy>(),
                services.GetRequiredService<ILogger<WebhookValidationHandler>>(),
                evaluateTarget: true))
            .AddHttpMessageHandler(services => new WebhookConnectionPoolHandler(WebhookHttpClientExtensions.CreateHandler(new WebhookConnection(
                services.GetRequiredService<IWebhookHostResolver>(),
                services.GetRequiredService<IExternalServiceTargetPolicy>()))));
        return builder;
    }
}
