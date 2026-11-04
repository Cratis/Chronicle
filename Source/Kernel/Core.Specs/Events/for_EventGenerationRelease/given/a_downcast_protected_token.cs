// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.given;

public class a_downcast_protected_token : Chronicle.EventSequences.Migrations.for_ProtectedEventTypeMigrations.given.a_token_protected_only_in_the_latest_generation
{
    protected EventGenerationRelease _release;
    protected AppendedEvent _event;
    protected AppendedEvent[] _result;
    protected EventType _pin = new("token-issued", 1);
    protected IDictionary<EventType, EventTypeSchema> _pinnedSchemas;

    async Task Establish()
    {
        _migrated = await _sut.MigratePlaintext(Store, Namespace, _eventType, _plaintext, _protectedSource, Subject, definition: _definition);
        _event = new(EventContext.Empty with { EventStore = Store, Namespace = Namespace, EventType = _eventType, Subject = Subject, AppendedGeneration = 3 }, _protectedSource)
        {
            GenerationalContent = _migrated.ToDictionary(_ => (int)_.Key.Value, _ => _converter.ToJsonObject(_.Value, _schemas[_.Key.Value - 1]).ToJsonString())
        };
        _pinnedSchemas = new Dictionary<EventType, EventTypeSchema> { [_pin] = new(_pin, EventTypeOwner.Server, EventTypeSource.Code, _schemas[0]) };
        _release = new(_storage, new EventCompliance(_manager, _converter), _migrations, _manager, _converter);
    }

    protected Task<AppendedEvent[]> Release() => _release.Release(Store, [_pin], _pinnedSchemas, [_event]);
    protected string ReleasedToken => (string)((IDictionary<string, object?>)_result[0].Content)["token"]!;
}
