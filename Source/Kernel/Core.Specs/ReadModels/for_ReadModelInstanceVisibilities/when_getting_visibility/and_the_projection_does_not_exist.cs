// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelInstanceVisibilities.when_getting_visibility;

public class and_the_projection_does_not_exist : given.a_visibility_resolver
{
    void Establish() => _projectionsManager.GetCurrent(EventStore, Namespace, ProjectionId).Returns((IProjection?)null);

    async Task Because() => _result = await _resolver.GetFor(EventStore, Namespace, DefinitionObservedBy(ReadModelObserverType.Projection));

    [Fact] void should_hide_uninitialized_roots() => _result.ShouldEqual(ReadModelInstanceVisibility.InitializedOnly);
}
