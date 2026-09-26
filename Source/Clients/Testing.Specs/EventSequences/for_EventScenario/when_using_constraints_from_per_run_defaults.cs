// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_using_constraints_from_per_run_defaults : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _result;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(LicenseIssued), typeof(LicenseReissued)]);
        artifacts.ConstraintTypes.Returns([typeof(UniqueLicenseKey)]);
        _scenario = new EventScenario(new Defaults(artifacts));
    }

    async Task Because()
    {
        var @event = new LicenseIssued(new LicenseKey(Guid.NewGuid()));
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(@event);
        _result = await _scenario.When.ForEventSource(EventSourceId.New()).Events(@event);
    }

    [Fact] void should_reject_a_duplicate_from_the_supplied_constraint() => _result.ShouldHaveConstraintViolation(UniqueLicenseKey.Name);

    public void Dispose() => _scenario?.Dispose();
}
