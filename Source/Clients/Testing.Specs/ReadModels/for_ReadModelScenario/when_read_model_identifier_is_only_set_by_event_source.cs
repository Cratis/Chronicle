// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// In production, MongoDB sets <c>_id</c> = projection key value and the BSON deserializer maps
/// <c>_id</c> back onto the read model's identifier property. The in-memory test harness skips
/// MongoDB, so it must mirror that mapping itself; otherwise <see cref="ReadModelScenario{T}.Instance"/>
/// returns a read model whose identifier property is <see langword="null"/> for any model whose ID
/// is sourced from the event-source ID (the common case).
/// </summary>
public class when_read_model_identifier_is_only_set_by_event_source : Specification
{
    [Passive]
    [FromEvent<KeyedSampleCreated>]
    public record KeyedSample([Key] Guid Id, string Name);

    [EventType]
    public record KeyedSampleCreated(string Name);

    ReadModelScenario<KeyedSample> _scenario;
    Guid _sampleGuid;
    EventSourceId _sampleId;

    void Establish()
    {
        _scenario = new ReadModelScenario<KeyedSample>();
        _sampleGuid = Guid.NewGuid();
        _sampleId = _sampleGuid.ToString();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_sampleId)
            .Events(new KeyedSampleCreated("Example"));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_populate_keyed_identifier_from_event_source() => _scenario.Instance.Id.ShouldEqual(_sampleGuid);
    [Fact] void should_still_populate_event_mapped_properties() => _scenario.Instance.Name.ShouldEqual("Example");
}
