// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Captures;

/// <summary>
/// Represents a command to save a capture. The capture's name is derived from the declaration.
/// </summary>
public class SaveCapture
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [FromRoute]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the capture - empty to create a new capture.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the capture declaration language source text.
    /// </summary>
    public string Declaration { get; set; } = string.Empty;
}
