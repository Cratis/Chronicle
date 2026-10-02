// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants;

public class and_round_tripping_the_matrix(ITestOutputHelper output)
{
    public static TheoryData<string, bool, string, string, bool> Cells => given.compliance_matrix.Cells;

    [Theory]
    [MemberData(nameof(Cells))]
    public Task should_differ_from_main_only_for_erased_read_model_updates(string shape, bool erased, string member, string protection, bool pipeline) =>
        given.differential.Compare(
            output,
            () =>
            {
                var specimen = given.compliance_matrix.Create(shape, member, protection);
                return (specimen.Schema, specimen.State);
            },
            erased,
            pipeline);
}
