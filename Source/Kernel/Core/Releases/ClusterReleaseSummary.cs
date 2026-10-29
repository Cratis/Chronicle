// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Releases;

/// <summary>
/// Represents a bounded summary of kernel releases, ordered by release.
/// </summary>
/// <param name="Releases">At most eight parsed releases and their silo counts.</param>
/// <param name="Unpublished">Silos without a valid published release.</param>
/// <param name="Omitted">Distinct releases omitted from the bounded list.</param>
public record ClusterReleaseSummary(IReadOnlyList<(SemanticVersion Release, int Silos)> Releases, int Unpublished, int Omitted)
{
    /// <summary>
    /// Gets whether more than one release, including unpublished releases, is present.
    /// </summary>
    public bool IsMixed => Releases.Count + (Unpublished > 0 ? 1 : 0) + Omitted > 1;

    /// <inheritdoc/>
    public override string ToString() => $"{string.Join(", ", Releases.Select(_ => $"{_.Release}: {_.Silos}"))}; unpublished: {Unpublished}; omitted: {Omitted}";
}
