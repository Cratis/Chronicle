// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_saving;

public class and_the_same_source_wins_an_insert_race : given.a_unique_constraints_storage
{
    Exception? _error;
    bool _oldAllowed;
    bool _newAllowed;
    EventSequenceNumber _sequenceNumber;

    void Establish() => _interceptors.Add(new InsertWinner(() => _storage.Save("owner", _definition, 42UL, "old")));

    async Task Because()
    {
        _error = await Catch.Exception(() => _storage.Save("owner", _definition, 43UL, "new"));
        (_oldAllowed, _) = await _storage.IsAllowed("other", _definition, "old");
        (_newAllowed, _sequenceNumber) = await _storage.IsAllowed("owner", _definition, "new");
    }

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
