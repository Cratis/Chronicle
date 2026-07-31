// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Captures;

/// <summary>
/// Represents a single observed item within a <see cref="CaptureObservation"/>.
/// </summary>
public class CaptureObservationItem
{
    /// <summary>
    /// Gets or sets the key identifying the item.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized JSON content of the item.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
