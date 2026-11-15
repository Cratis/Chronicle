// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

[Collection(SqlServerCollection.Name)]
public class when_retrying_archive_after_head_reuse_on_sql_server(SqlServerFixture fixture) : Specification
{
    static readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();
    static readonly EventStoreName _eventStore = "test-store";
    static readonly EventStoreNamespaceName _namespace = "test-namespace";

    string _connectionString;
    IDatabase _database;
    ArchiveRetryConformance _trace;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        _database = Substitute.For<IDatabase>();
        _database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), static () => { })));
        _trace = ArchiveRetryRequests.Create();
        await _trace.Establish(new EventSequenceMutationRegistry(_eventStore, _namespace, _database));
    }

    async Task Because() => await _trace.RetryArchive(new EventSequenceMutationRegistry(_eventStore, _namespace, _database));

    async Task Destroy() => await fixture.QueueDatabaseDrop(_connectionString);

    NamespaceDbContext CreateContext() => new(Database.BuildOptions<NamespaceDbContext>(_services, _connectionString));

    [Fact] void should_be_already_archived() => _trace.ShouldBeAlreadyArchived();
    [Fact] void should_return_the_original_receipt() => _trace.ShouldReturnOriginalReceipt();
    [Fact] void should_return_a_verified_receipt() => _trace.ShouldReturnVerifiedReceipt();
    [Fact] void should_keep_the_next_mutation_resumable() => _trace.ShouldKeepNextMutationResumable();
    [Fact] void should_keep_the_next_token_unchanged() => _trace.ShouldKeepNextToken();
    [Fact] void should_keep_the_next_ordinal_unchanged() => _trace.ShouldKeepNextOrdinal();
    [Fact] void should_keep_the_next_mutation_unchanged() => _trace.ShouldKeepNextMutation();
}
