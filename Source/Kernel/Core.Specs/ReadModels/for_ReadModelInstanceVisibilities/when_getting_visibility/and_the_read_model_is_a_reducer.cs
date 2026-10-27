// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelInstanceVisibilities.when_getting_visibility;

public class and_the_read_model_is_a_reducer : given.a_visibility_resolver
{
    async Task Because() => _result = await _resolver.GetFor(EventStore, Namespace, DefinitionObservedBy(ReadModelObserverType.Reducer));

    [Fact] void should_hide_uninitialized_roots() => _result.ShouldEqual(ReadModelInstanceVisibility.InitializedOnly);
    [Fact] void should_not_look_up_a_projection() => _projectionsManager.DidNotReceive().GetCurrent(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<Cratis.Chronicle.Concepts.Projections.ProjectionId>());
}
