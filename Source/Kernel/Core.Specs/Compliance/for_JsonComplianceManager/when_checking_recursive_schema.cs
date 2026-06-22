// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

public class when_checking_recursive_schema : Specification
{
    record TreeNode(string Id, IReadOnlyList<TreeNode> Children);

    bool _result;

    void Because() => _result = JsonSchema.FromType<TreeNode>().HasComplianceMetadata();

    [Fact] void should_not_hang_or_overflow() => _result.ShouldBeFalse();
}
