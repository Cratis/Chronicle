// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Attributes;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Identities;

/// <summary>
/// Represents a MongoDB representation of an identity.
/// </summary>
public class MongoDBIdentity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [BsonId]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;
}
