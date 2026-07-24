// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelDiffer.when_diffing;

public class a_new_instance : given.a_differ
{
    IReadOnlyList<MaterializedReadModelChange> _result;

    void Because() => _result = _differ.Diff([new MaterializedItem("1", "first")]);

    [Fact] void should_detect_a_single_change() => _result.Count.ShouldEqual(1);
    [Fact] void should_detect_an_addition() => _result[0].ChangeType.ShouldEqual(ReadModelChangeType.Added);
    [Fact] void should_carry_the_key() => _result[0].ModelKey.ShouldEqual("1");
}
