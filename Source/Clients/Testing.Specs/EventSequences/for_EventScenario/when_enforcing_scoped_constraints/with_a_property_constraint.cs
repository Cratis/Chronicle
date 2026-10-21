// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_enforcing_scoped_constraints;

/// <summary>
/// A unique property constraint scoped per event stream treats the same value in another stream as a different value.
/// Were the scope dropped on the way to the in-process kernel, the value would be unique across all streams and the
/// append in the second stream would be refused.
/// </summary>
public class with_a_property_constraint : Specification, IDisposable
{
    static readonly EventStreamType _streamType = new("rota");

    EventScenario _scenario;
    AppendResult _firstInFirstStream;
    AppendResult _duplicateInFirstStream;
    AppendResult _duplicateInSecondStream;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(ScopedBadgeIssued)]);
        artifacts.ConstraintTypes.Returns([typeof(UniqueBadgeNumberPerStream)]);
        _scenario = new EventScenario(new Defaults(artifacts));
    }

    async Task Because()
    {
        _firstInFirstStream = await _scenario.EventLog.Append(EventSourceId.New(), new ScopedBadgeIssued("B-1"), _streamType, new EventStreamId("monday"));
        _duplicateInFirstStream = await _scenario.EventLog.Append(EventSourceId.New(), new ScopedBadgeIssued("B-1"), _streamType, new EventStreamId("monday"));
        _duplicateInSecondStream = await _scenario.EventLog.Append(EventSourceId.New(), new ScopedBadgeIssued("B-1"), _streamType, new EventStreamId("tuesday"));
    }

    [Fact] void should_accept_the_first_badge_number() => _firstInFirstStream.ShouldBeSuccessful();
    [Fact] void should_reject_the_same_badge_number_in_the_same_stream() => _duplicateInFirstStream.ShouldHaveConstraintViolation(UniqueBadgeNumberPerStream.Name);
    [Fact] void should_accept_the_same_badge_number_in_another_stream() => _duplicateInSecondStream.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
