// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents the last observed state of a capture's source - the baseline the next capture cycle diffs against.
/// Persisted so a capture resumes from where it left off when the kernel restarts.
/// </summary>
/// <param name="Id">The <see cref="CaptureId"/> the observation belongs to.</param>
/// <param name="Items">The items as they were last observed.</param>
public record CaptureObservation(CaptureId Id, IReadOnlyList<CaptureObservedItem> Items)
{
    /// <summary>
    /// Creates an empty <see cref="CaptureObservation"/> for a capture that has not observed anything yet.
    /// </summary>
    /// <param name="id">The <see cref="CaptureId"/>.</param>
    /// <returns>An empty <see cref="CaptureObservation"/>.</returns>
    public static CaptureObservation Empty(CaptureId id) => new(id, []);
}
