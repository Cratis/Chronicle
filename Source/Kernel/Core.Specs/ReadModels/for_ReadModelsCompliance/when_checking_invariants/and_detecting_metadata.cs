// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants;

public class and_detecting_metadata(ITestOutputHelper output)
{
    public static TheoryData<string, string, string> Cells
    {
        get
        {
            var cells = new TheoryData<string, string, string>();
            var combinations = from shape in given.compliance_matrix.Shapes
                               from member in given.compliance_matrix.Members
                               from protection in given.compliance_matrix.Protections
                               select (shape, member, protection);
            foreach (var (shape, member, protection) in combinations) cells.Add(shape, member, protection);
            return cells;
        }
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public Task should_preserve_main_without_a_guard_marker(string shape, string member, string protection) =>
        given.differential.Compare(
            output,
            () =>
            {
                var specimen = given.compliance_matrix.Create(shape, member, protection, includeGuard: false);
                return (specimen.Schema, specimen.State);
            },
            false);
}
