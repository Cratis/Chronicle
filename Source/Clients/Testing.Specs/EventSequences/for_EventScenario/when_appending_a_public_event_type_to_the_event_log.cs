// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A deployed kernel refuses a public event type on the event log, so a scenario has to refuse it too: otherwise a
/// suite stays green while every such append fails at runtime.
/// </summary>
public class when_appending_a_public_event_type_to_the_event_log : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _result;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(MembershipAnnounced)]);
        _scenario = new EventScenario(new Defaults(artifacts));
    }

    async Task Because() => _result = await _scenario.EventLog.Append(EventSourceId.New(), new MembershipAnnounced("alice"));

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_refuse_the_public_event_type() => _result.Errors.Select(error => error.Value).ShouldContain("PublicEventTypeCannotBeAppendedToEventLog");

    public void Dispose() => _scenario.Dispose();
}
