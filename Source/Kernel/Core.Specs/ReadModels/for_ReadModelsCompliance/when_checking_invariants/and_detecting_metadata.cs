// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants;

public class and_detecting_metadata
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
    public void should_report_only_metadata_present_in_the_schema(string shape, string member, string protection)
    {
        var schema = given.compliance_matrix.Create(shape, member, protection, includeGuard: false).Schema;
        var expected = protection == "pii";
        Assert.True(schema.HasSchemaMetadata() == expected, $"INVARIANTS: I5\nExpected metadata: {expected}");
        Assert.False(schema.HasSchemaMetadata(SchemaMetadataCategory.Security), "INVARIANTS: I5\nNo security metadata is declared");
    }
}
