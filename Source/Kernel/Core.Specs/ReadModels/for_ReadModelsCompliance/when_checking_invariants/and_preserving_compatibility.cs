// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants;

public class and_preserving_compatibility(ITestOutputHelper output)
{
    public static TheoryData<string, bool> Cells => given.compatibility_matrix.Cells;

    [Theory]
    [MemberData(nameof(Cells))]
    public Task should_preserve_the_reference_union_legacy_and_append_cells(string cell, bool erased) =>
        given.differential.Compare(
            output,
            () => given.compatibility_matrix.Create(cell),
            erased,
            subject: cell.StartsWith("subject/", StringComparison.Ordinal) ? string.Empty : "subject",
            legacyPaths: given.compatibility_matrix.LegacyPaths(cell),
            reducer: cell.StartsWith("union/", StringComparison.Ordinal) || cell.StartsWith("legacy/", StringComparison.Ordinal));
}
