// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Holds the log messages for <see cref="MongoDBReadModels"/>.
/// </summary>
internal static partial class MongoDBReadModelsLogMessages
{
    [LoggerMessage(0, LogLevel.Debug, "Adding binding for IMongoCollection<{Type}> using collection {CollectionName}")]
    internal static partial void AddingMongoDBCollectionBinding(this ILogger logger, Type type, string collectionName);
}
