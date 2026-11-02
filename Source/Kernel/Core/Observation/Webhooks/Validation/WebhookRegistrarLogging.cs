// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks;

internal static partial class WebhookRegistrarLogging
{
    [LoggerMessage(LogLevel.Warning, "Webhook registration probe failed")]
    internal static partial void ProbeFailed(this ILogger<WebhookRegistrar> logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Webhook '{Identifier}' is already registered with the same delivery format, so the registration is skipped")]
    internal static partial void IdenticalWebhookAlreadyRegistered(this ILogger<WebhookRegistrar> logger, string identifier);
}
