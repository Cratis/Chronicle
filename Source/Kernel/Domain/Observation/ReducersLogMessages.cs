// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Domain.Observation;

internal static partial class ReducersLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Registering client Reducers")]
    internal static partial void RegisterReducers(this ILogger<Reducers> logger);
}
