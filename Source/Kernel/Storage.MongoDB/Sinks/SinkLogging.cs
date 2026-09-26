// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

internal static partial class SinkLogging
{
    [LoggerMessage(LogLevel.Information, "Repairing legacy null parent for read model '{ReadModel}' before retrying a MongoDB update")]
    internal static partial void RepairingNullParent(this ILogger<Sink> logger, ReadModelIdentifier readModel);
}
