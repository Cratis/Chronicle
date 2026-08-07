// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

internal static partial class ChangesetConverterLogging
{
    [LoggerMessage(LogLevel.Debug, "Join for read model '{ReadModel}' on property '{OnProperty}' with key '{Key}' matched no documents")]
    internal static partial void JoinMatchedNoDocuments(this ILogger<ChangesetConverter> logger, ReadModelIdentifier readModel, PropertyPath onProperty, object key);
}
