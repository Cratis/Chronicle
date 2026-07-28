// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelDiffer.when_diffing;

public class a_modified_instance : given.a_differ
{
    IReadOnlyList<MaterializedReadModelChange> _result;

    void Establish() => _differ.Diff([new MaterializedItem("1", "first")]);

    void Because() => _result = _differ.Diff([new MaterializedItem("1", "second")]);

    [Fact] void should_detect_a_single_change() => _result.Count.ShouldEqual(1);
    [Fact] void should_detect_a_modification() => _result[0].ChangeType.ShouldEqual(ReadModelChangeType.Modified);
}
