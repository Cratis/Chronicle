// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// Warns at startup about ignored development-only webhook settings.
/// </summary>
/// <param name="options">The kernel options.</param>
/// <param name="environment">The runtime environment.</param>
/// <param name="logger">The logger.</param>
public class WebhookValidationStartup(IOptions<ChronicleOptions> options, IHostEnvironment environment, ILogger<WebhookValidationStartup> logger) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Value.Webhooks.AllowInsecureHttp && !environment.IsDevelopment()) logger.InsecureHttpIgnored();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
