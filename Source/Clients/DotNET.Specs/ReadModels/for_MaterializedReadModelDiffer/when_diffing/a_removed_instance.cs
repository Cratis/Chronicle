// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelDiffer.when_diffing;

public class a_removed_instance : given.a_differ
{
    IReadOnlyList<MaterializedReadModelChange> _result;

    void Establish() => _differ.Diff([new MaterializedItem("1", "first")]);

    void Because() => _result = _differ.Diff([]);

    [Fact] void should_detect_a_single_change() => _result.Count.ShouldEqual(1);
    [Fact] void should_detect_a_removal() => _result[0].ChangeType.ShouldEqual(ReadModelChangeType.Removed);
    [Fact] void should_carry_the_removed_key() => _result[0].ModelKey.ShouldEqual("1");
}
