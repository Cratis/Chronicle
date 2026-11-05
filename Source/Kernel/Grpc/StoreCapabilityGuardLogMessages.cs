// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Log messages for <see cref="StoreCapabilityGuard"/>.
/// </summary>
internal static partial class StoreCapabilityGuardLogMessages
{
    [LoggerMessage(LogLevel.Information, "Capability {Capability} is not granted to the caller (reason {Reason}); the query is allowed for this event store")]
    internal static partial void StoreCapabilityNotGranted(this ILogger logger, string capability, string reason);
}
