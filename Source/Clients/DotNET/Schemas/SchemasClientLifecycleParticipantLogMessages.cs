// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Schemas;

internal static partial class SchemasClientLifecycleParticipantLogMessages
{
    [LoggerMessage(1, LogLevel.Information, "Registering event types")]
    internal static partial void RegisterEventTypes(this ILogger<SchemasClientLifecycleParticipant> logger);
}
