// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces;

/// <summary>
/// Represents a MongoDB representation of a namespace.
/// </summary>
public class MongoDBNamespace
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [BsonId]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the namespace.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when it was created.
    /// </summary>
    public DateTimeOffset Created { get; set; }
}
