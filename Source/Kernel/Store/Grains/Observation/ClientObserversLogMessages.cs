// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Log messages for <see cref="ClientObservers"/>.
/// </summary>
public static partial class ClientObserversLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Connected client with identifier '{ConnectionId}' disconnected")]
    internal static partial void Disconnected(this ILogger logger, string connectionId);
}
