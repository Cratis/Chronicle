// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Captures;

/// <summary>
/// Provides extension methods for converting between Kernel and SQL capture representations.
/// </summary>
public static class CaptureConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.Captures.Capture"/> to a SQL <see cref="Capture"/>.
    /// </summary>
    /// <param name="capture">The Kernel capture.</param>
    /// <returns>The SQL capture.</returns>
    public static Capture ToSql(this Concepts.Captures.Capture capture) =>
        new()
        {
            Id = capture.Id.Value.ToString(),
            Name = capture.Name,
            Declaration = capture.Declaration,
            Status = capture.Status
        };

    /// <summary>
    /// Converts a SQL <see cref="Capture"/> to a Kernel <see cref="Concepts.Captures.Capture"/>.
    /// </summary>
    /// <param name="capture">The SQL capture.</param>
    /// <returns>The Kernel capture.</returns>
    public static Concepts.Captures.Capture ToKernel(this Capture capture) =>
        new(
            new CaptureId(Guid.Parse(capture.Id)),
            capture.Name,
            capture.Declaration,
            capture.Status);

    /// <summary>
    /// Converts a Kernel <see cref="CaptureObservation"/> to a SQL <see cref="CaptureObservationEntry"/>.
    /// </summary>
    /// <param name="observation">The Kernel capture observation.</param>
    /// <returns>The SQL capture observation entry.</returns>
    public static CaptureObservationEntry ToSql(this CaptureObservation observation) =>
        new()
        {
            Id = observation.Id.Value.ToString(),
            Items = observation.Items.ToDictionary(item => item.Key, item => item.Content)
        };

    /// <summary>
    /// Converts a SQL <see cref="CaptureObservationEntry"/> to a Kernel <see cref="CaptureObservation"/>.
    /// </summary>
    /// <param name="entry">The SQL capture observation entry.</param>
    /// <returns>The Kernel capture observation.</returns>
    public static CaptureObservation ToKernel(this CaptureObservationEntry entry) =>
        new(
            new CaptureId(Guid.Parse(entry.Id)),
            entry.Items.Select(item => new CaptureObservedItem(item.Key, item.Value)).ToList());
}
