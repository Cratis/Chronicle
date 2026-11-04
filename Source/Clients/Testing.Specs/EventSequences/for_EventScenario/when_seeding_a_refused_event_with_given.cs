// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_seeding_a_refused_event_with_given : Specification, IDisposable
{
    EventScenario _scenario;
    Exception _exception;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(MembershipAnnounced)]);
        _scenario = new EventScenario(new Defaults(artifacts));
    }

    async Task Because() => _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(new MembershipAnnounced("alice")));

    [Fact] void should_throw_given_event_append_failed() => _exception.ShouldBeOfExactType<GivenEventAppendFailed>();
    [Fact] void should_name_the_event_type() => _exception.Message.ShouldContain(nameof(MembershipAnnounced));
    [Fact] void should_name_the_error() => _exception.Message.ShouldContain("PublicEventTypeCannotBeAppendedToEventLog");

    public void Dispose() => _scenario.Dispose();
}
