// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Stores the kernel storage lease using BSON dates.
/// </summary>
internal class SingleWriterLeaseDocument
{
    /// <summary>
    /// Gets or sets the Id.
    /// </summary>
    [BsonId]
    public string Id { get; set; } = "kernel";

    /// <summary>
    /// Gets or sets the Owner.
    /// </summary>
    [BsonElement("owner")]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ClusterId.
    /// </summary>
    [BsonElement("clusterId")]
    public string ClusterId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ServiceId.
    /// </summary>
    [BsonElement("serviceId")]
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HostName.
    /// </summary>
    [BsonElement("hostName")]
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Version.
    /// </summary>
    [BsonElement("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AcquiredAt.
    /// </summary>
    [BsonElement("acquiredAt")]
    public DateTime AcquiredAt { get; set; }

    /// <summary>
    /// Gets or sets the RenewedAt.
    /// </summary>
    [BsonElement("renewedAt")]
    public DateTime RenewedAt { get; set; }

    /// <summary>
    /// Gets or sets the ExpiresAt.
    /// </summary>
    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }
}
