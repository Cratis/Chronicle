// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_registering_concurrently;

public class on_sqlite : given.a_sqlite_database
{
    readonly MeterListener _listener = new();
    readonly Activity _activity = new("registration-race");
    (int Count, bool IsReplayable) _saved;
    int _collisions;

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "chronicle-sql-upsert-collisions-recovered") listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((_, value, tags, _) =>
        {
            if (ReferenceEquals(Activity.Current, _activity) && tags.ToArray().Any(tag => tag.Key == "operation" && Equals(tag.Value, "tracked-upsert")))
            {
                _collisions += value;
            }
        });
        _listener.Start();
    }

    async Task Because()
    {
        using var activity = _activity.Start();
        _saved = await given.registration_race.Run(_database, differingPayload: true);
    }

    void Destroy()
    {
        _listener.Dispose();
        _activity.Dispose();
    }

    [Fact] void should_count_one_recovered_collision() => _collisions.ShouldEqual(1);
    [Fact] void should_keep_one_registration() => _saved.Count.ShouldEqual(1);
    [Fact] void should_store_the_last_completed_writer() => _saved.IsReplayable.ShouldBeFalse();
}
