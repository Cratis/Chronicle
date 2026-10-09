// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Concepts.Sinks.for_EventSequenceSinkConfiguration.when_resolving;

public class and_the_exact_generation_is_registered : Specification
{
    EventSequenceSinkConfiguration _configuration;
    EventTypeSchema _registered;
    EventTypeSchema _result;

    void Establish()
    {
        _registered = new(new EventType("PublicStateChanged", new EventTypeGeneration(7)), default, default, new JsonSchema());
        _configuration = new(_registered.Type);
    }

    void Because() => _result = _configuration.ResolveTarget([_registered with { Type = new("PublicStateChanged", new EventTypeGeneration(8)) }, _registered]);

    [Fact] void should_use_the_exact_registered_schema() => _result.ShouldEqual(_registered);
    [Fact] void should_default_to_outbox() => _configuration.Destination.ShouldEqual(EventSequenceId.Outbox);
}
