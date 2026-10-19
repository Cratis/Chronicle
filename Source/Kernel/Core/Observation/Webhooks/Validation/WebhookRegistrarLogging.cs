// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks;

internal static partial class WebhookRegistrarLogging
{
    [LoggerMessage(LogLevel.Warning, "Webhook registration probe failed")]
    internal static partial void ProbeFailed(this ILogger<WebhookRegistrar> logger, Exception exception);
}
