// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.SideEffects;

internal static partial class ReactorEffectKeysLogging
{
    [LoggerMessage(LogLevel.Warning, "Reactor '{ReactorId}' returned events that are appended without idempotency keys because the connected Chronicle kernel does not advertise idempotent appends. A recovered partition or a replay appends them again. Upgrade the kernel to make them idempotent")]
    internal static partial void UnkeyedReturnedEvents(this ILogger logger, ReactorId reactorId);
}
