// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceDelivery.given;

public class a_queue : Specification
{
    protected readonly TriggerId _trigger = new("trigger");
    protected readonly DateTimeOffset _now = DateTimeOffset.UnixEpoch.AddDays(1);
    protected Storage.InMemory.Occurrences.OccurrencesStorage _storage;
    protected Storage.InMemory.Occurrences.TriggerDefinitionsStorage _definitions;
    protected OccurrenceDelivery _queue;
    protected Clock _clock;

    async Task Establish()
    {
        _storage = new();
        _definitions = new();
        _clock = new() { Now = _now };
        await _definitions.TrySave(new(_trigger, "Trigger", "{\"type\":\"object\",\"required\":[\"value\"],\"properties\":{\"value\":{\"type\":\"integer\"}}}", 4), null);
        _queue = new(_storage, _definitions, _trigger, new(), _clock);
    }

    protected class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
