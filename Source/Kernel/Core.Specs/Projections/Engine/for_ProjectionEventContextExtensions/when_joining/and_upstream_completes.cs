// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_joining;

public class and_upstream_completes : given.a_projection_event_context_observable
{
    bool _completed;
    IObservable<ProjectionEventContext> _join;

    void Establish()
    {
        _join = _subject.Join(PropertyPath.Root);
        _join.Subscribe(_ => { }, () => _completed = true);
    }

    void Because() => _subject.OnCompleted();

    [Fact] void should_propagate_completion_downstream() => _completed.ShouldBeTrue();
}
