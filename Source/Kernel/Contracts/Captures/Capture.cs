// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Captures;

/// <summary>
/// Represents a capture.
/// </summary>
[ProtoContract]
public class Capture
{
    /// <summary>
    /// Gets or sets the unique identifier of the capture.
    /// </summary>
    [ProtoMember(1)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the capture.
    /// </summary>
    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the capture declaration language source text.
    /// </summary>
    [ProtoMember(3)]
    public string Declaration { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <see cref="CaptureStatus"/>.
    /// </summary>
    [ProtoMember(4)]
    public CaptureStatus Status { get; set; }
}
