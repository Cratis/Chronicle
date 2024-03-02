// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Projections;

internal static partial class ProjectionsRegistrarLogMessages
{
    [LoggerMessage(1, LogLevel.Information, "Registering projections")]
    internal static partial void RegisterProjections(this ILogger<ProjectionsRegistrar> logger);
}
