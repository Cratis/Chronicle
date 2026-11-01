// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Validates HTTP capture destinations independently of webhook receiver settings.
/// </summary>
/// <param name="options">The kernel configuration.</param>
/// <param name="environment">The kernel runtime environment.</param>
/// <param name="resolver">The host resolver.</param>
/// <param name="logger">The logger.</param>
public class ExternalServiceTargetPolicy(
    IOptions<ChronicleOptions> options,
    IHostEnvironment environment,
    IWebhookHostResolver resolver,
    ILogger<WebhookTargetPolicy> logger) : WebhookTargetPolicy(options.Value.ExternalServices, ("Capture source", "ExternalServices"), resolver, environment, logger), IExternalServiceTargetPolicy;
