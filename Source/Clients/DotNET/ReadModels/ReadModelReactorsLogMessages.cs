// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

internal static partial class ReadModelReactorsLogMessages
{
    [LoggerMessage(LogLevel.Error, "Failed to activate read model reactor of type '{ReactorType}'")]
    internal static partial void FailedActivatingReadModelReactor(this ILogger<ReadModelReactors> logger, string reactorType, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed dispatching read model change to reactor of type '{ReactorType}'")]
    internal static partial void FailedDispatchingReadModelChange(this ILogger<ReadModelReactors> logger, string reactorType, Exception exception);
}
