// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Verifies that a unique constraint whose key is a composite of two <c>ConceptAs&lt;Guid&gt;</c> values
/// registers and enforces: re-appending the same (request + consultant) pair is rejected.
/// </summary>
public class when_violating_a_composite_concept_constraint : Specification, IDisposable
{
    EventScenario _scenario;
    RequestId _request;
    ConsultantId _consultant;
    AppendResult _result;

    void Establish()
    {
        _scenario = new EventScenario();
        _request = new(Guid.NewGuid());
        _consultant = new(Guid.NewGuid());
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(EventSourceId.New())
            .Events(new CandidateSubmittedForRequest(_request, _consultant));

        _result = await _scenario.When
            .ForEventSource(EventSourceId.New())
            .Events(new CandidateSubmittedForRequest(_request, _consultant));
    }

    [Fact] void should_have_failed() => _result.ShouldBeFailed();
    [Fact] void should_have_the_constraint_violation() => _result.ShouldHaveConstraintViolation(UniqueRequestConsultant.Name);

    public void Dispose() => _scenario.Dispose();
}
