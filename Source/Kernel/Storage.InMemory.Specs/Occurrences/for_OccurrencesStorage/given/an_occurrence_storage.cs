// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences.for_OccurrencesStorage.given;

public class an_occurrence_storage : Specification
{
    protected readonly TriggerId _trigger = new(Guid.NewGuid().ToString("N"));
    protected readonly DateTimeOffset _now = DateTimeOffset.UnixEpoch.AddDays(1);
    protected ScheduleToken _token;
    protected IOccurrencesStorage _storage;
    protected IScheduleStorage _schedules;
    protected Occurrence _occurrence;
    OccurrencesStorage _concrete;

    protected IOccurrencesStorage Recreate() => _concrete;
    protected IScheduleStorage RecreateSchedules() => _concrete;

    void Establish()
    {
        _concrete = new();
        _storage = _concrete;
        _schedules = _concrete;
        _token = new ScheduleToken(_trigger.Value);
        _occurrence = new(Guid.NewGuid().ToString("N"), _trigger, OccurrenceKind.Application, "first", "{\"value\":1}", 1, _now, null, null, OccurrenceState.Pending, 0, 0, null, null, null, null);
    }
}
