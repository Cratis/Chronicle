// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_acting_with_when_and_violating_a_fluent_concept_constraint : Specification, IDisposable
{
    EventScenario _scenario;
    LicenseKey _key;
    AppendResult _result;

    void Establish()
    {
        _scenario = new EventScenario();
        _key = new(Guid.NewGuid());
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(EventSourceId.New())
            .Events(new LicenseIssued(_key));

        _result = await _scenario.When
            .ForEventSource(EventSourceId.New())
            .Events(new LicenseReissued(_key));
    }

    [Fact] void should_have_failed() => _result.ShouldBeFailed();
    [Fact] void should_have_the_constraint_violation() => _result.ShouldHaveConstraintViolation(UniqueLicenseKey.Name);

    public void Dispose() => _scenario.Dispose();
}
