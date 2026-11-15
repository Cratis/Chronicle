// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_a_tag_resolver : a_named_tag_append
{
    protected IEventTagEnricher _enricher;

    void Establish()
    {
        _enricher = Substitute.For<IEventTagEnricher>();
        _enricher.GetTags(Arg.Any<EventTagContext>()).Returns(call => [new NamedTag(new TagName("enriched"), (string)call.Arg<EventTagContext>().Event)]);
        _eventSequence = new(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            _connection,
            _eventTypes,
            _constraints,
            _eventSerializer,
            _correlationIdAccessor,
            _concurrencyScopeStrategies,
            _causationManager,
            _unitOfWorkManager,
            _identityProvider,
            JsonSerializerOptions.Default,
            tagResolver: new EventTagResolver([_enricher]));
    }
}
