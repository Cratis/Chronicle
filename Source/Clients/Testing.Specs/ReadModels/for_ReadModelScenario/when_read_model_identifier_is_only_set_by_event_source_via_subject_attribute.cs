// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Variant of <see cref="when_read_model_identifier_is_only_set_by_event_source"/> where the identifier
/// is marked with <see cref="SubjectAttribute"/> instead of <see cref="Keys.KeyAttribute"/> — both
/// should populate from the event source ID in the test harness, matching MongoDB's _id-mapping
/// precedence ([Key] → [Subject] → property named "Id").
/// </summary>
public class when_read_model_identifier_is_only_set_by_event_source_via_subject_attribute : Specification
{
    [Passive]
    [FromEvent<SubjectedSampleCreated>]
    public record SubjectedSample([Subject] Guid Id, string Name);

    [EventType]
    public record SubjectedSampleCreated(string Name);

    ReadModelScenario<SubjectedSample> _scenario;
    Guid _sampleGuid;
    EventSourceId _sampleId;

    void Establish()
    {
        _scenario = new ReadModelScenario<SubjectedSample>();
        _sampleGuid = Guid.NewGuid();
        _sampleId = _sampleGuid.ToString();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_sampleId)
            .Events(new SubjectedSampleCreated("Example"));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_populate_subjected_identifier_from_event_source() => _scenario.Instance.Id.ShouldEqual(_sampleGuid);
}
