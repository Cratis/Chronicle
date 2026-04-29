// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Log messages for <see cref="ReadModels"/>.
/// </summary>
internal static partial class ReadModelsLogMessages
{
    [LoggerMessage(LogLevel.Error, "Failed to release compliance for read model '{ReadModelType}' with subject '{Subject}': {Error}")]
    internal static partial void FailedToRelease(this ILogger<ReadModels> logger, string readModelType, string subject, string error);
}
