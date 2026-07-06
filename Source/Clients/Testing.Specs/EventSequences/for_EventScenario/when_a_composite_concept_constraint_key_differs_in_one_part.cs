// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Verifies that the composite key is genuinely composite: the same request with a different consultant
/// is allowed, proving both ConceptAs parts participate in the uniqueness key rather than just the first.
/// </summary>
public class when_a_composite_concept_constraint_key_differs_in_one_part : Specification, IDisposable
{
    EventScenario _scenario;
    RequestId _request;
    ConsultantId _firstConsultant;
    ConsultantId _secondConsultant;
    AppendResult _result;

    void Establish()
    {
        _scenario = new EventScenario();
        _request = new(Guid.NewGuid());
        _firstConsultant = new(Guid.NewGuid());
        _secondConsultant = new(Guid.NewGuid());
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(EventSourceId.New())
            .Events(new CandidateSubmittedForRequest(_request, _firstConsultant));

        _result = await _scenario.When
            .ForEventSource(EventSourceId.New())
            .Events(new CandidateSubmittedForRequest(_request, _secondConsultant));
    }

    [Fact] void should_have_succeeded() => _result.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
