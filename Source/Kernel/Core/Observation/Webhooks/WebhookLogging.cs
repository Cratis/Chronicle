// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Holds log messages for <see cref="Webhook"/>.
/// </summary>
internal static partial class WebhookLogging
{
    [LoggerMessage(LogLevel.Debug, "Setting webhook definition and subscribing for webhook '{Identifier}'")]
    internal static partial void SettingDefinition(this ILogger<Webhook> logger, WebhookId identifier);

    [LoggerMessage(LogLevel.Information, "Webhook '{Identifier}' is a new webhook")]
    internal static partial void WebhookIsNew(this ILogger<Webhook> logger, WebhookId identifier);

    [LoggerMessage(LogLevel.Information, "Registering webhook '{Identifier}' has changed its definition")]
    internal static partial void WebhookHasChanged(this ILogger<Webhook> logger, WebhookId identifier);
}
