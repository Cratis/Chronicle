// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Holds log messages for <see cref="WebhooksManager"/>.
/// </summary>
internal static partial class WebhooksManagerLogging
{
    [LoggerMessage(LogLevel.Information, "Setting definition for webhook '{Identifier}'")]
    internal static partial void SettingDefinition(this ILogger<WebhooksManager> logger, WebhookId identifier);

    [LoggerMessage(LogLevel.Information, "Subscribing webhook '{Identifier}' in namespace '{Namespace}'")]
    internal static partial void Subscribing(this ILogger<WebhooksManager> logger, WebhookId identifier, EventStoreNamespaceName @namespace);
}
