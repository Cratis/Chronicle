// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelInstanceVisibilities.when_getting_visibility;

public class and_the_projection_is_not_yet_cached : given.a_visibility_resolver
{
    void Establish() => _projectionsManager.GetDefinition(EventStore, ProjectionId).Returns(_projection);

    async Task Because() => _result = await _resolver.GetFor(EventStore, Namespace, DefinitionObservedBy(ReadModelObserverType.Projection));

    [Fact] void should_include_uninitialized_roots() => _result.ShouldEqual(ReadModelInstanceVisibility.IncludeUninitialized);
    [Fact] async Task should_not_compile_a_graph() => await _projectionsManager.DidNotReceive().GetCurrent(EventStore, Namespace, ProjectionId);
}
