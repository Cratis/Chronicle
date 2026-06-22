// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

public class when_checking_recursive_schema : Specification
{
    record TreeNode(string Id, IReadOnlyList<TreeNode> Children);

    bool _fromTypeResult;
    bool _selfReferencingArrayResult;

    async Task Because()
    {
        // A read model whose child collection is of its own type. The array-item traversal added
        // for list-valued PII must terminate rather than recurse forever — and recognising the
        // cycle relies on resolving each reference to its shared definition, because the schema
        // accessors otherwise hand back fresh wrapper objects on every access (which is exactly
        // what crashed projection read models with deep/recursive hierarchies).
        _fromTypeResult = JsonSchema.FromType<TreeNode>().HasComplianceMetadata();

        var selfReferencingArray = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "children": { "type": "array", "items": { "$ref": "#" } }
              }
            }
            """);
        _selfReferencingArrayResult = selfReferencingArray.HasComplianceMetadata();
    }

    [Fact] void should_terminate_for_a_recursive_type() => _fromTypeResult.ShouldBeFalse();
    [Fact] void should_terminate_for_a_self_referencing_array() => _selfReferencingArrayResult.ShouldBeFalse();
}
