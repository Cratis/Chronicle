// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Storage.MongoDB.Captures;

/// <summary>
/// Represents the MongoDB representation of a capture observation.
/// </summary>
public class CaptureObservation
{
    /// <summary>
    /// Gets or sets the <see cref="CaptureId"/> the observation belongs to.
    /// </summary>
    public CaptureId Id { get; set; } = CaptureId.NotSet;

    /// <summary>
    /// Gets or sets the observed items.
    /// </summary>
    public IList<CaptureObservationItem> Items { get; set; } = [];
}
