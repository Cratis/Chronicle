// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Integration;

internal static partial class AdaptersConnectionLifecycleParticipantLogMessages
{
    [LoggerMessage(1, LogLevel.Information, "Registering adapters")]
    internal static partial void RegisterAdapters(this ILogger<AdaptersConnectionLifecycleParticipant> logger);
}
