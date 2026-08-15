// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Compliance;

/// <summary>
/// Represents the request for authorizing a new encryption key for a subject whose key was erased.
/// </summary>
[ProtoContract]
public class AllowNewEncryptionKeyRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event store namespace.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encryption key identifier (the subject the PII was encrypted under).
    /// </summary>
    [ProtoMember(3)]
    public string Identifier { get; set; } = string.Empty;
}
