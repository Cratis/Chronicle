// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Compliance;

/// <summary>
/// Represents the response for a <see cref="ReleaseRequest"/> — either the decrypted payload or an error.
/// </summary>
[ProtoContract]
public class ReleaseResponse
{
    /// <summary>
    /// Gets or sets the decrypted JSON payload. Empty when <see cref="HasError"/> is <see langword="true"/>.
    /// </summary>
    [ProtoMember(1)]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the release failed.
    /// </summary>
    [ProtoMember(2)]
    public bool HasError { get; set; }

    /// <summary>
    /// Gets or sets the error message when <see cref="HasError"/> is <see langword="true"/>.
    /// </summary>
    [ProtoMember(3)]
    public string Error { get; set; } = string.Empty;
}
