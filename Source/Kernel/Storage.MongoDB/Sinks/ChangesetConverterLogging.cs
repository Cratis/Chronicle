// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

internal static partial class ChangesetConverterLogging
{
    [LoggerMessage(LogLevel.Debug, "Root-level join for read model '{ReadModel}' on property '{OnProperty}' matched no documents")]
    internal static partial void JoinMatchedNoDocuments(this ILogger<ChangesetConverter> logger, ReadModelIdentifier readModel, PropertyPath onProperty);

    [LoggerMessage(LogLevel.Debug, "Join for read model '{ReadModel}' on property '{OnProperty}' has no value to filter on and was not written")]
    internal static partial void JoinHasNoKey(this ILogger<ChangesetConverter> logger, ReadModelIdentifier readModel, PropertyPath onProperty);

    [LoggerMessage(LogLevel.Debug, "The key of a join for read model '{ReadModel}' does not convert through the schema of property '{OnProperty}'; filtering on the unconverted value, which matches nothing")]
    internal static partial void JoinKeyNotConvertible(this ILogger<ChangesetConverter> logger, Exception exception, ReadModelIdentifier readModel, PropertyPath onProperty);
}
