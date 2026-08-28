// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

internal static partial class MongoDBConverterLogging
{
    // The exception is deliberately not passed. ILogger renders exception.ToString(), and a Guid-formatted
    // property against a value that is not a Guid embeds the offending value in the parse failure - which
    // can be the compliance subject. The read model and the property are the diagnostic value; the value
    // itself is not.
    [LoggerMessage(LogLevel.Warning, "The value for property '{Property}' of read model '{ReadModel}' does not convert through its declared schema format; storing the untyped value instead")]
    internal static partial void KeyNotConvertible(this ILogger<MongoDBConverter> logger, ReadModelIdentifier readModel, string property);
}
