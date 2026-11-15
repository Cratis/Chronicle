// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.SequenceQueries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.SequenceQueries.given;

/// <summary>
/// Describes who a stored query or folder belongs to, relative to the caller.
/// </summary>
public enum StoredOwner
{
    /// <summary>The stored item belongs to the caller.</summary>
    Caller = 0,

    /// <summary>The stored item belongs to an owner that only differs from the caller in letter case.</summary>
    CallerInAnotherCase = 1,

    /// <summary>The stored item belongs to somebody else.</summary>
    SomeoneElse = 2,

    /// <summary>Nothing is stored under the identifier.</summary>
    None = 3
}

/// <summary>
/// A command scenario over a store that already holds a query and a folder, owned as described.
/// </summary>
/// <typeparam name="TCommand">The command under test.</typeparam>
/// <param name="isAdministrator">Whether the caller holds administrator privileges.</param>
/// <param name="stored">Who the stored items belong to.</param>
/// <param name="scope">The scope the stored items are shared with.</param>
public abstract class a_saved_query_store<TCommand>(bool isAdministrator, StoredOwner stored, SequenceQueryScope scope = SequenceQueryScope.User) : Specification
{
    protected readonly CommandScenario<TCommand> _scenario = ChronicleCommandScenario.For<TCommand>(isAdministrator);
    protected ISequenceQueryStorage _queries;
    protected string _storedOwner;
    protected CommandResult _result;

    void Establish()
    {
        var accessor = (ICurrentPrincipalAccessor)_scenario.Services.Last(_ => _.ServiceType == typeof(ICurrentPrincipalAccessor)).ImplementationInstance!;
        var caller = SequenceQueryOwners.GetCurrent(accessor);
        _storedOwner = stored switch
        {
            StoredOwner.Caller => caller,
            StoredOwner.CallerInAnotherCase => caller.ToUpperInvariant(),
            _ => "someone-else"
        };

        _queries = Substitute.For<ISequenceQueryStorage>();
        if (stored != StoredOwner.None)
        {
            _queries.Get(Arg.Any<SequenceQueryId>()).Returns(
                new Concepts.SequenceQueries.SequenceQueryDefinition(
                    "query-1",
                    "Stored",
                    scope,
                    _storedOwner,
                    string.Empty,
                    "default",
                    "event-log",
                    Concepts.SequenceQueries.SequenceQueryFilter.Empty,
                    SequenceQuerySortBy.Occurred,
                    Descending: false));
            _queries.GetFolder(Arg.Any<SequenceQueryFolderId>()).Returns(
                new SequenceQueryFolderDefinition("folder-1", scope, _storedOwner, "default", "stored"));
        }

        var eventStore = Substitute.For<IEventStoreStorage>();
        eventStore.SequenceQueries.Returns(_queries);
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Destroy() => await _scenario.DisposeAsync();
}
