// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Captures;

/// <summary>
/// Represents the payload for starting a capture.
/// </summary>
[ProtoContract]
public class StartCapture
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the capture.
    /// </summary>
    [ProtoMember(2)]
    public string Id { get; set; } = string.Empty;
}
