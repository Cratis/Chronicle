// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Webhooks;

internal static partial class WebhooksLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Registering webhook with id '{WebhookId}', for event sequence '{EventSequenceId}'")]
    internal static partial void RegisterWebhook(this ILogger<Webhooks> logger, WebhookId webhookId, EventSequenceId eventSequenceId);
}
