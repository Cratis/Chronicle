// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModelDiffer.when_diffing;

public class an_unchanged_instance : given.a_differ
{
    IReadOnlyList<MaterializedReadModelChange> _result;

    void Establish() => _differ.Diff([new MaterializedItem("1", "first")]);

    void Because() => _result = _differ.Diff([new MaterializedItem("1", "first")]);

    [Fact] void should_detect_no_changes() => _result.ShouldBeEmpty();
}
