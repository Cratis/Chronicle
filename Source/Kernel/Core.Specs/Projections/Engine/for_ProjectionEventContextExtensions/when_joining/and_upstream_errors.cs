// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_joining;

public class and_upstream_errors : given.a_projection_event_context_observable
{
    Exception _received;
    IObservable<ProjectionEventContext> _join;
    Exception _error;

    void Establish()
    {
        _error = new InvalidOperationException("upstream failure");
        _join = _subject.Join(PropertyPath.Root);
        _join.Subscribe(_ => { }, ex => _received = ex);
    }

    void Because() => _subject.OnError(_error);

    [Fact] void should_propagate_error_downstream() => _received.ShouldEqual(_error);
}
