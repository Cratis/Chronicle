// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleasePlan.when_building_the_plan;

/// <summary>
/// The scan that finds declarations below the read model walks the property graph, and a read model may hold
/// a child collection of its own child type. It has to terminate, and it has to stay silent about a graph
/// that declares nothing — the scan runs for every read model, declared or not.
/// </summary>
public class and_the_graph_is_recursive : Specification
{
    record Node(string Name, IReadOnlyList<Node> Children);

    record Tree(string Id, [PII] string Owner, Node Root, IReadOnlyList<Node> Orphans);

    Exception _error;
    ReadModelReleasePlan _result;

    void Because()
    {
        _error = Catch.Exception(() => _result = ReadModelReleasePlan.For(typeof(Tree)));
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_not_have_declarations() => _result.HasDeclarations.ShouldBeFalse();
}
