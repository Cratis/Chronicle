// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that <see cref="ReadModelScenario{TReadModel}"/> reflects a root-level removal: a class-level
/// <c>[RemovedWith]</c> deletes the read model document in the real sink, so <c>Instance</c> resolves to
/// <see langword="null"/> rather than the stale pre-removal state.
/// </summary>
public class when_removing_the_root_read_model : Specification
{
    ReadModelScenario<RemovableWidget> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _id = EventSourceId.New();
        _scenario = new ReadModelScenario<RemovableWidget>();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(new RemovableWidgetCreated("My Widget"), new RemovableWidgetDeleted());

    [Fact] void should_reflect_the_removal() => _scenario.Instance.ShouldBeNull();
}
