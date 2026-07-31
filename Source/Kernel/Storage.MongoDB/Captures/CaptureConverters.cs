// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Captures;

/// <summary>
/// Provides extension methods for converting between Kernel and MongoDB capture representations.
/// </summary>
public static class CaptureConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.Captures.Capture"/> to a MongoDB <see cref="Capture"/>.
    /// </summary>
    /// <param name="capture">The Kernel capture.</param>
    /// <returns>The MongoDB capture.</returns>
    public static Capture ToMongoDB(this Concepts.Captures.Capture capture) =>
        new()
        {
            Id = capture.Id,
            Name = capture.Name,
            Declaration = capture.Declaration,
            Status = capture.Status
        };

    /// <summary>
    /// Converts a MongoDB <see cref="Capture"/> to a Kernel <see cref="Concepts.Captures.Capture"/>.
    /// </summary>
    /// <param name="capture">The MongoDB capture.</param>
    /// <returns>The Kernel capture.</returns>
    public static Concepts.Captures.Capture ToKernel(this Capture capture) =>
        new(
            capture.Id,
            capture.Name,
            capture.Declaration,
            capture.Status);

    /// <summary>
    /// Converts a Kernel <see cref="Concepts.Captures.CaptureObservation"/> to a MongoDB <see cref="CaptureObservation"/>.
    /// </summary>
    /// <param name="observation">The Kernel capture observation.</param>
    /// <returns>The MongoDB capture observation.</returns>
    public static CaptureObservation ToMongoDB(this Concepts.Captures.CaptureObservation observation) =>
        new()
        {
            Id = observation.Id,
            Items = observation.Items.Select(item => new CaptureObservationItem { Key = item.Key, Content = item.Content }).ToList()
        };

    /// <summary>
    /// Converts a MongoDB <see cref="CaptureObservation"/> to a Kernel <see cref="Concepts.Captures.CaptureObservation"/>.
    /// </summary>
    /// <param name="observation">The MongoDB capture observation.</param>
    /// <returns>The Kernel capture observation.</returns>
    public static Concepts.Captures.CaptureObservation ToKernel(this CaptureObservation observation) =>
        new(
            observation.Id,
            observation.Items.Select(item => new Concepts.Captures.CaptureObservedItem(item.Key, item.Content)).ToList());
}
