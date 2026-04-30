// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Compliance;

/// <summary>
/// Represents the request for releasing (decrypting) PII fields in a JSON payload.
/// </summary>
[ProtoContract]
public class ReleaseRequest
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
    /// Gets or sets the subject used as the encryption key identifier.
    /// </summary>
    [ProtoMember(3)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON schema of the payload, used to locate compliance-annotated properties.
    /// </summary>
    [ProtoMember(4)]
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON payload to decrypt.
    /// </summary>
    [ProtoMember(5)]
    public string Payload { get; set; } = string.Empty;
}
