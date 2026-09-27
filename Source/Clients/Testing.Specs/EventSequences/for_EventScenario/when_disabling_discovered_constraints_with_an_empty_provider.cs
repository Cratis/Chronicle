// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_disabling_discovered_constraints_with_an_empty_provider : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _result;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(SubscriptionActivated)]);
        artifacts.UniqueEventTypeConstraints.Returns([typeof(SubscriptionActivated)]);
        var emptyProvider = Substitute.For<ICanProvideConstraints>();
        emptyProvider.Provide().Returns(ImmutableList<IConstraintDefinition>.Empty);
        _scenario = new EventScenario(new Defaults(artifacts), emptyProvider);
    }

    async Task Because()
    {
        var id = EventSourceId.New();
        await _scenario.When.ForEventSource(id).Events(new SubscriptionActivated("pro"));
        _result = await _scenario.When.ForEventSource(id).Events(new SubscriptionActivated("pro"));
    }

    [Fact] void should_accept_the_second_event() => _result.ShouldBeSuccessful();

    public void Dispose() => _scenario?.Dispose();
}
