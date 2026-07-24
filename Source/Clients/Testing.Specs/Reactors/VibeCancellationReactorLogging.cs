// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Testing.Reactors;

internal static partial class VibeCancellationReactorLogging
{
    [LoggerMessage(LogLevel.Information, "Vibe hosted by '{Host}' was cancelled")]
    internal static partial void VibeCancelled(this ILogger<VibeCancellationReactor> logger, string host);
}
