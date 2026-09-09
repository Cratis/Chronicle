// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry.given;

public class a_controlled_registry_interleaving : a_mutation_registry
{
    protected IEventSequenceMutationRegistry BeforeNextWrite(Func<Task> interleave)
    {
        var interceptor = new before_write(interleave);
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(_ =>
        {
            var options = new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(_connection)
                .AddConceptAsSupport().AddInterceptors(interceptor).Options;
            return Task.FromResult(new DbContextScope<NamespaceDbContext>(new(options), () => { }));
        });
        return new EventSequenceMutationRegistry(_eventStore, _namespace, database);
    }

    protected async Task<EventSequenceMutationRegistryTransitionResult> Commit(IEventSequenceMutationRegistry registry, EventSequenceMutationBeginResult begin)
    {
        var applying = await Apply(registry, begin, EventSequenceMutationTransition.BeginApplying);
        var verifying = await registry.Transition(_target, applying.Token!, EventSequenceMutationTransition.BeginVerifying);
        return await registry.Transition(_target, verifying.Token!, EventSequenceMutationTransition.CommitSourceWithoutRepair);
    }

    sealed class before_write(Func<Task> interleave) : DbCommandInterceptor
    {
        Func<Task>? _interleave = interleave;

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("UPDATE", StringComparison.Ordinal) && Interlocked.Exchange(ref _interleave, null) is { } action)
            {
                await action();
            }

            return result;
        }
    }
}
