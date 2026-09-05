// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry.given;

/// <summary>
/// Provides an <see cref="EventSequenceMutationRegistry"/> backed by a real, isolated MongoDB database for one
/// spec run, together with the request-building helpers the in-memory reference specs use.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> providing the MongoDB server.</param>
public abstract class a_mutation_registry(MongoDBFixture fixture) : Specification
{
    IMongoClient _client = default!;
    string _databaseName = default!;

    /// <summary>
    /// Gets the database the registry under test persists to.
    /// </summary>
    protected IEventStoreNamespaceDatabase Database { get; private set; } = default!;

    /// <summary>
    /// Gets the registry under test.
    /// </summary>
    protected IEventSequenceMutationRegistry Registry { get; private set; } = default!;

    /// <summary>
    /// Gets the identity of the target event sequence every spec begins a mutation against.
    /// </summary>
    protected EventSequenceMutationIdentity Target { get; private set; } = default!;

    /// <summary>
    /// Gets the target range proposed for a winning registration.
    /// </summary>
    protected EventSequenceMutationTarget ProposedTarget { get; private set; } = default!;

    /// <summary>
    /// Gets the default mutation request specs begin against <see cref="Target"/>.
    /// </summary>
    protected EventSequenceMutationRequest Request { get; private set; } = default!;

    /// <summary>
    /// Creates an <see cref="EventSequenceMutationRegistry"/> over the given real MongoDB database, matching the
    /// constructor shape the registry is used with at runtime.
    /// </summary>
    /// <param name="database">The <see cref="IEventStoreNamespaceDatabase"/> to construct the registry over.</param>
    /// <returns>The constructed registry.</returns>
    protected static IEventSequenceMutationRegistry RegistryOver(IEventStoreNamespaceDatabase database) =>
        new EventSequenceMutationRegistry("event-store", "namespace", database);

    /// <summary>
    /// Creates an event sequence mutation identity from a display string.
    /// </summary>
    /// <param name="display">The display form.</param>
    /// <returns>The created identity.</returns>
    protected static EventSequenceMutationIdentity Identity(string display) => EventSequenceMutationIdentity.TryCreate(display).Identity!;

    /// <summary>
    /// Builds a mutation request targeting the given identity, matching the shape and defaults the in-memory
    /// reference specs use.
    /// </summary>
    /// <param name="target">The target event sequence identity.</param>
    /// <param name="originSequenceNumber">The originating event's sequence number.</param>
    /// <param name="payload">The command payload text.</param>
    /// <param name="hash">The command hash text.</param>
    /// <returns>The built request.</returns>
    protected static EventSequenceMutationRequest BuildRequest(
        EventSequenceMutationIdentity target,
        ulong originSequenceNumber = 42,
        string payload = "{\"name\":\"Ada\"}",
        string hash = "command-hash")
    {
        var origin = Identity("origin-sequence");
        const EventSequenceMutationKind kind = EventSequenceMutationKind.Revision;
        var id = EventSequenceMutationDigestCalculator.CalculateId(target, origin, originSequenceNumber, kind);
        return new(id, target, new(origin, originSequenceNumber), kind, new(payload, hash));
    }

    /// <summary>
    /// Applies a transition to the successor of a begin result, mirroring the shape callers use once they hold a
    /// token.
    /// </summary>
    /// <param name="registry">The registry to transition on.</param>
    /// <param name="begin">The begin result carrying the predecessor token.</param>
    /// <param name="transition">The transition to apply.</param>
    /// <returns>The transition result.</returns>
    protected static async Task<EventSequenceMutationRegistryTransitionResult> Apply(
        IEventSequenceMutationRegistry registry,
        EventSequenceMutationBeginResult begin,
        EventSequenceMutationTransition transition) =>
        await registry.Transition(begin.Active!.TargetSequence, begin.Token!, transition);

    void Establish()
    {
        _databaseName = $"chr_mutation_registry_{Guid.NewGuid():N}";
        _client = new MongoClient(fixture.ConnectionString);
        var mongoDatabase = _client.GetDatabase(_databaseName);

        Database = Substitute.For<IEventStoreNamespaceDatabase>();
        Database.GetCollection<EventSequenceMutationHeadEntry>(WellKnownCollectionNames.EventSequenceMutationHeads)
            .Returns(mongoDatabase.GetCollection<EventSequenceMutationHeadEntry>(WellKnownCollectionNames.EventSequenceMutationHeads));
        Database.GetCollection<EventSequenceMutationHistoryEntry>(WellKnownCollectionNames.EventSequenceMutationHistory)
            .Returns(mongoDatabase.GetCollection<EventSequenceMutationHistoryEntry>(WellKnownCollectionNames.EventSequenceMutationHistory));

        Registry = RegistryOver(Database);
        Target = Identity("target-sequence");
        ProposedTarget = new(10UL, 13UL, 3UL);
        Request = BuildRequest(Target);
    }

    async Task Destroy()
    {
        if (_databaseName is not null)
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }
}
