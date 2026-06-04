// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

internal static partial class MaterializedReadModelsLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Failed to release read model '{ReadModelType}' for subject '{Subject}'. Error: {Error}")]
    internal static partial void FailedToRelease(this ILogger<MaterializedReadModels> logger, string readModelType, string subject, string error);
}
