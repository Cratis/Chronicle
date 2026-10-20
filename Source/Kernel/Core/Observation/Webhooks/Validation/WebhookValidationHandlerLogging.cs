// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

internal static partial class WebhookValidationHandlerLogging
{
    [LoggerMessage(LogLevel.Debug, "Webhook request has no event-store option; applying global target policy")]
    internal static partial void UsingGlobalPolicy(this ILogger<WebhookValidationHandler> logger);
}
