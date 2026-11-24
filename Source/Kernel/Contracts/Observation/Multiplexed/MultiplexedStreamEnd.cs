// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Multiplexed;

/// <summary>
/// Represents the end of one stream of a multiplexed observation connection.
/// </summary>
/// <remarks>
/// A stream that ends with <see cref="StatusCode"/> zero completed, which is what a per-observer call completing looks
/// like. Any other value is what a per-observer call failing would have raised, with the same gRPC status, so a client
/// classifies it the way it classifies a failed per-observer call. The connection itself stays open.
/// </remarks>
[ProtoContract]
public class MultiplexedStreamEnd
{
    /// <summary>
    /// Gets or sets the gRPC status code the stream ended with. Zero (<c language="csharp">OK</c>) means it completed.
    /// </summary>
    [ProtoMember(1)]
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the status detail, empty when the stream completed.
    /// </summary>
    [ProtoMember(2)]
    public string Detail { get; set; } = string.Empty;
}
