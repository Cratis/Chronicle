// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.EventSequences.Outbox;

internal static partial class OutboxProjectionsRegistrarLogMessages
{
    [LoggerMessage(1, LogLevel.Information, "Registering outbox projections")]
    internal static partial void RegisteringOutboxProjections(this ILogger<OutboxProjectionsRegistrar> logger);
}
