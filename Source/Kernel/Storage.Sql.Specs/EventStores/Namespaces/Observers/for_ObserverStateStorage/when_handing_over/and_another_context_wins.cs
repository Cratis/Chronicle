// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using KernelState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_handing_over;

public class and_another_context_wins : given.an_observer_state_storage
{
    ObserverStateStorage _loser;
    KernelState _result;
    bool _accepted;
    bool _winnerAccepted;

    async Task Establish()
    {
        await _storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(999) }, "A");
        var interceptor = new before_update(async () =>
        {
            // A second context wins after the losing operation has reached its database write.
            _winnerAccepted = await _storage.HandOver("observer", "A", ObserverSequenceCheckpoint.Fresh("observer", "C") with { NextEventSequenceNumber = new(77) });
            await using var context = CreateContext();
            await context.Observers.Where(row => row.Id == "observer")
                .ExecuteUpdateAsync(set => set.SetProperty(row => row.AlertRevision, 31L));
        });
        var options = new DbContextOptionsBuilder<NamespaceDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .WithoutServiceProviderCaching()
            .AddInterceptors(interceptor)
            .Options;
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(new NamespaceDbContext(options), () => { })));
        _loser = new("orders", "default", database);
    }

    async Task Because()
    {
        _accepted = await _loser.HandOver("observer", "A", ObserverSequenceCheckpoint.Fresh("observer", "B"));
        _result = await _storage.Get("observer");
    }

    [Fact] void should_accept_the_winner() => _winnerAccepted.ShouldBeTrue();
    [Fact] void should_reject_the_loser() => _accepted.ShouldBeFalse();
    [Fact] void should_keep_the_winning_owner() => _result.Owner.ShouldEqual(new EventSequenceId("C"));
    [Fact] void should_keep_the_winning_progress() => _result.NextEventSequenceNumber.Value.ShouldEqual(77UL);
    [Fact] void should_keep_the_concurrent_alert_change() => _result.AlertRevision.ShouldEqual(31L);

    sealed class before_update(Func<Task> beforeUpdate) : DbCommandInterceptor
    {
        bool _called;

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!_called && command.CommandText.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                _called = true;
                await beforeUpdate();
            }

            return result;
        }
    }
}
