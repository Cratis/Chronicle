// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

public sealed class batch_collision_counter : IDisposable
{
    readonly Activity _activity = new Activity("batch-recovery-spec").Start();
    readonly MeterListener _listener = new();

    public int Count { get; private set; }

    public batch_collision_counter()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "chronicle-sql-upsert-collisions-recovered") listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((_, count, tags, _) =>
        {
            if (ReferenceEquals(Activity.Current, _activity) && tags.ToArray().Any(tag => tag.Key == "operation" && Equals(tag.Value, "unique-constraint-batch"))) Count += count;
        });
        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
        _activity.Dispose();
    }
}
