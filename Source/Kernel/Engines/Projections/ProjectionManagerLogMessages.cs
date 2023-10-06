// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Engines.Projections;

internal static partial class ProjectionManagerLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Registering projection '{Name} ({Identifier})'")]
    internal static partial void Registering(this ILogger<ProjectionManager> logger, ProjectionId identifier, ProjectionName name);
}
