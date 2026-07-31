// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ExternalServices;
using Microsoft.Extensions.DependencyInjection;

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
        builder.Services.AddHttpClient(ExternalServiceHttpClientFactory.HttpClientName);
        return builder;
    }
}
