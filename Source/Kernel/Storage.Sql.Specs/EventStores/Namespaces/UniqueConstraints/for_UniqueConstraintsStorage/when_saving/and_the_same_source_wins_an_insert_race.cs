// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_saving;

public class and_the_same_source_wins_an_insert_race : given.a_unique_constraints_storage
{
    readonly MeterListener _listener = new();
    readonly Activity _activity = new("insert-race");
    int _collisions;
    Exception? _error;
    bool _oldAllowed;
    bool _newAllowed;
    EventSequenceNumber _sequenceNumber;

    void Establish()
    {
        _interceptors.Add(new InsertWinner(() => _storage.Save("owner", _definition, 42UL, "old")));
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "cratis.chronicle.sql.upsert.recovered_collisions") listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((_, value, tags, _) =>
        {
            if (ReferenceEquals(Activity.Current, _activity) && tags.ToArray().Any(tag => tag.Key == "operation" && Equals(tag.Value, "unique-constraint-source")))
            {
                _collisions += value;
            }
        });
        _listener.Start();
    }

    void Destroy()
    {
        _listener.Dispose();
        _activity.Dispose();
    }

    async Task Because()
    {
        using var activity = _activity.Start();
        _error = await Catch.Exception(() => _storage.Save("owner", _definition, 43UL, "new"));
        (_oldAllowed, _) = await _storage.IsAllowed("other", _definition, "old");
        (_newAllowed, _sequenceNumber) = await _storage.IsAllowed("owner", _definition, "new");
    }

    [Fact] void should_count_the_recovered_collision_once() => _collisions.ShouldEqual(1);

    [Fact] void should_retry_the_insert_as_an_update() => _error.ShouldBeNull();
    [Fact] void should_release_the_racing_claim() => _oldAllowed.ShouldBeTrue();
    [Fact] void should_keep_the_requested_claim() => _newAllowed.ShouldBeTrue();
    [Fact] void should_save_the_requested_sequence_number() => _sequenceNumber.ShouldEqual((EventSequenceNumber)43UL);

    sealed class InsertWinner(Func<Task> insertWinner) : SaveChangesInterceptor
    {
        bool _inserted;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!_inserted && eventData.Context!.ChangeTracker.Entries<UniqueConstraintIndexEntry>().Any(entry => entry.State == EntityState.Added))
            {
                _inserted = true;
                await insertWinner();
            }

            return result;
        }
    }
}
