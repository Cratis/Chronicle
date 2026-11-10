// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Represents a manager of PII in the system.
/// </summary>
/// <remarks>
/// <para>
/// Initializes a new instance of the <see cref="PIIManager"/> class.
/// </para>
/// <para>
/// Every operation here runs across every event store in the manager's namespace. That is not a convenience: the
/// cross-event-store subscription copies a subject's key into the store it forwards into whenever that store has
/// none, so a key erased in one event store is put back by the next forwarded event unless the erasure reached the
/// store that still had it - and reached it in a way that refuses the copy afterwards.
/// </para>
/// </remarks>
/// <param name="keyStore">The <see cref="IEncryptionKeyStorage"/>.</param>
/// <param name="cacheClient">The <see cref="IEncryptionKeyCacheClient"/> used to evict the key from every silo's cache.</param>
/// <param name="storage">The <see cref="IStorage"/> used to enumerate the event stores an erasure has to reach.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/> the compliance acts are recorded to.</param>
/// <param name="erasureOptions">Release grace configuration.</param>
/// <param name="delay">Optional injectable retry and grace delay.</param>
/// <param name="random">Optional retry jitter source.</param>
public class PIIManager(
    IEncryptionKeyStorage keyStore,
    IEncryptionKeyCacheClient cacheClient,
    IStorage storage,
    ILogger<PIIManager> logger,
    EncryptionKeyErasureOptions? erasureOptions = null,
    Func<TimeSpan, Task>? delay = null,
    Random? random = null) : Grain, IPIIManager
{
    /// <summary>
    /// The most event stores one step of an operation reaches at the same time.
    /// </summary>
    internal const int MaxConcurrentEventStores = 16;

    readonly EncryptionKeyErasureOptions _erasureOptions = erasureOptions ?? new();
    readonly Func<TimeSpan, Task> _delay = delay ?? (duration => Task.Delay(duration));
    readonly Random _random = random ?? Random.Shared;

    /// <inheritdoc/>
    public async Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier)
    {
        ThrowIfEncryptedValueIdentifier(identifier);
        var key = GetKey();
        List<Exception> failures = [];
        var eventStores = await EventStoresToReach(key, failures);
        await DeleteEncryptionKey(identifier, key, eventStores, failures, null);
    }

    /// <inheritdoc/>
    public Task DeleteEncryptionKeyIn(EncryptionKeyIdentifier identifier, EventStoreName[] eventStores)
    {
        ThrowIfEncryptedValueIdentifier(identifier);
        var key = GetKey();
        return DeleteEncryptionKey(identifier, key, AuthorizedStores(key, eventStores), [], null);
    }

    /// <inheritdoc/>
    public Task DeleteEncryptionKeyInWithRequest(EncryptionKeyIdentifier identifier, EventStoreName[] eventStores, string requestId)
    {
        ThrowIfEncryptedValueIdentifier(identifier);
        EncryptionKeyErasureRequestIdentity.Validate(requestId);
        var key = GetKey();
        return DeleteEncryptionKey(identifier, key, AuthorizedStores(key, eventStores), [], requestId);
    }

    /// <inheritdoc/>
    public async Task AllowNewEncryptionKeyFor(EncryptionKeyIdentifier identifier)
    {
        ThrowIfEncryptedValueIdentifier(identifier);
        var key = GetKey();
        List<Exception> failures = [];
        var eventStores = await EventStoresToReach(key, failures);
        await AllowNewEncryptionKey(identifier, key, eventStores, failures);
    }

    /// <inheritdoc/>
    public Task AllowNewEncryptionKeyIn(EncryptionKeyIdentifier identifier, EventStoreName[] eventStores)
    {
        ThrowIfEncryptedValueIdentifier(identifier);
        var key = GetKey();
        return AllowNewEncryptionKey(identifier, key, AuthorizedStores(key, eventStores), []);
    }

    static void ThrowIfEncryptedValueIdentifier(EncryptionKeyIdentifier identifier)
    {
        // Defense in depth: the PII path never constructs an identifier carrying the reserved marker (see
        // EncryptedValueKeyIdentifiers), so this can only be reached when a caller on the compliance gRPC surface
        // - the one entry point that accepts an arbitrary caller-supplied identifier - addresses an erasure or
        // authorization at one directly.
        if (EncryptedValueKeyIdentifiers.IsEncryptedValueIdentifier(identifier))
        {
            throw new EncryptionKeyIsNotErasable(identifier);
        }
    }

    static string Names(IEnumerable<EventStoreName> eventStores) => string.Join(", ", eventStores.Select(_ => _.Value));

    static string BindingFor(EncryptionKeyIdentifier identifier)
    {
        // The act is logged, the person is not. Chronicle deliberately does not name the data subject in its logs -
        // a line naming an erased person is unencrypted personal data that outlives the crypto-shred, and unlike the
        // erasure fence, which lives inside the key store and is destroyed with it, a log line travels to
        // aggregators whose retention and access sit outside the deployment's data boundary. The binding is a
        // stable one-way derivation of the identifier, so an operator who already knows the subject can compute it
        // and find the act; it just does not hand the name to everyone who can read a log. It is a pseudonym rather
        // than an anonymization - a low-entropy identifier space is brute-forceable - which is exactly the trade a
        // pseudonymous binding is meant to make.
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identifier.Value)));
    }

    static bool IsSystem(EventStoreName eventStore) => eventStore.Value.Equals(EventStoreName.System.Value, StringComparison.OrdinalIgnoreCase);

    static EventStoreName[] AuthorizedStores(PIIManagerKey key, IEnumerable<EventStoreName> eventStores) =>
        eventStores.Where(eventStore => !IsSystem(eventStore) || IsSystem(key.EventStore)).Distinct().ToArray();

    /// <summary>
    /// Run one step of an operation against every event store, recording the failures in event store order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every event store is attempted even after one fails. Stopping at the first would leave the key alive in a store
    /// that was never reached, and the caller sees the same failure either way.
    /// </para>
    /// <para>
    /// The stores of one step are reached concurrently, and the step completes only when every store has answered, so
    /// the order between steps - every fence raised before any sweep, every sweep before eviction, the grace before any
    /// confirmation - is exactly what it would be one store at a time. Each store is a separate database and nothing a
    /// step does in one store depends on another store within the same step. Reaching them one at a time made an erasure
    /// grow with the number of event stores until it outlasted the grain call timeout.
    /// </para>
    /// </remarks>
    /// <param name="eventStores">The event stores to reach.</param>
    /// <param name="failures">The failures of the operation as a whole, appended to in event store order.</param>
    /// <param name="operation">The step to run against one event store.</param>
    /// <param name="unreachable">Optional set that event stores whose fence did not answer in time are added to.</param>
    /// <returns>Awaitable task.</returns>
    static async Task ForEach(IEnumerable<EventStoreName> eventStores, List<Exception> failures, Func<EventStoreName, Task> operation, HashSet<EventStoreName>? unreachable = null)
    {
        var outcomes = await Across(eventStores, async eventStore =>
        {
            await operation(eventStore);
            return true;
        });

        foreach (var (eventStore, _, error) in outcomes)
        {
            if (error is null) continue;

            // A store whose fence did not answer in time is not asked again during this call: each further step would
            // wait for the same timeout, and together they would outlast the caller.
            if (unreachable is not null && IsUnavailable(error)) unreachable.Add(eventStore);
            failures.Add(error);
        }
    }

    /// <summary>
    /// Run an operation against every event store concurrently, at most <see cref="MaxConcurrentEventStores"/> at a time.
    /// </summary>
    /// <remarks>
    /// Every operation is started from, and every continuation resumes on, the calling grain's scheduler; only the
    /// storage calls themselves overlap. The outcomes are collected per store rather than written to shared state, and
    /// are returned in the order the event stores were given.
    /// </remarks>
    /// <typeparam name="TResult">Type of result of the operation.</typeparam>
    /// <param name="eventStores">The event stores to reach.</param>
    /// <param name="operation">The operation to run against one event store.</param>
    /// <returns>The outcome for every event store, in the order given.</returns>
    static async Task<(EventStoreName EventStore, TResult? Result, Exception? Error)[]> Across<TResult>(IEnumerable<EventStoreName> eventStores, Func<EventStoreName, Task<TResult>> operation)
    {
        using var slots = new SemaphoreSlim(MaxConcurrentEventStores);
        return await Task.WhenAll(eventStores.Select(async eventStore =>
        {
            await slots.WaitAsync();
            try
            {
                return (eventStore, await operation(eventStore), (Exception?)null);
            }
            catch (Exception error)
            {
                return (eventStore, default(TResult), error);
            }
            finally
            {
                slots.Release();
            }
        }));
    }

    static EventStoreName[] Reachable(IEnumerable<EventStoreName> eventStores, HashSet<EventStoreName> unreachable) =>
        [.. eventStores.Where(eventStore => !unreachable.Contains(eventStore))];

    static bool IsUnavailable(Exception error) => error is EncryptionKeyFenceUnavailable or EncryptionKeyStorageUnavailable;

    /// <summary>
    /// Decide whether a failed erasure step is tried again.
    /// </summary>
    /// <remarks>
    /// Contention clears within milliseconds. An unavailable store has already spent a bounded wait, so it gets one more
    /// try rather than seven: retries multiplied by that wait are what held a grain past its call timeout.
    /// A write whose outcome is unknown (<see cref="EncryptionKeyFenceWriteOutcomeUnknown"/>) is retried the same way: every
    /// retried step carries the same request id, which the store deduplicates, and a final failure still fails closed.
    /// </remarks>
    /// <param name="error">The failure.</param>
    /// <param name="attempt">The zero-based attempt that failed.</param>
    /// <returns><see langword="true"/> to try again.</returns>
    static bool ShouldRetry(Exception error, int attempt) =>
        (error is EncryptionKeyFenceChanged && attempt < 7) || (IsUnavailable(error) && attempt < 1);

    async Task DeleteEncryptionKey(EncryptionKeyIdentifier identifier, PIIManagerKey key, IReadOnlyList<EventStoreName> eventStores, List<Exception> failures, string? requestId)
    {
        if (SupportsProtocolErasure())
        {
            await DeleteProtocolEncryptionKey(identifier, key, eventStores, failures, requestId);
            return;
        }

        if (requestId is not null)
        {
            // Retry identities promise immutable boundaries. A legacy provider cannot make that promise.
            failures.Add(new EncryptionKeyFenceUnavailable(identifier));
            CompleteErasure(identifier, key, eventStores, failures);
            return;
        }

        await DeleteLegacyEncryptionKey(identifier, key, eventStores, failures);
    }

    async Task DeleteProtocolEncryptionKey(EncryptionKeyIdentifier identifier, PIIManagerKey key, IReadOnlyList<EventStoreName> eventStores, List<Exception> failures, string? requestId)
    {
        // A store whose fence did not answer in time is not asked again during this call: each further step would wait
        // for the same timeout, and together they would outlast the caller. The erasure reports it as incomplete instead.
        var unreachable = new HashSet<EventStoreName>();
        requestId ??= await PendingRequest(identifier, key, eventStores, unreachable, failures) ?? Guid.NewGuid().ToString("N");
        var request = new EncryptionKeyErasureRequest(requestId);

        // Fence every reachable store before sweeping any. Never substitute an unbounded delete on failure.
        await ForEach(Reachable(eventStores, unreachable), failures, eventStore => Retry(() => keyStore.RecordErasureFor(eventStore, key.Namespace, identifier, request)), unreachable);
        await ForEach(
            Reachable(eventStores, unreachable),
            failures,
            async eventStore =>
            {
                var recorded = await keyStore.GetErasureRequestFor(eventStore, key.Namespace, identifier, requestId);
                if (recorded?.Confirmed == true) return;
                await Retry(() => keyStore.DeleteThroughFor(eventStore, key.Namespace, identifier, requestId));
                await RemoveUniqueClaimsFor(eventStore, key.Namespace, identifier);
            },
            unreachable);

        // Evict even after a partial storage failure. Confirmation is only legal after eviction and the grace.
        await ForEach(eventStores, failures, eventStore => cacheClient.Evict(eventStore, key.Namespace, identifier));
        if (failures.Count == 0)
        {
            // One grace for the erasure as a whole, never one per store: every store was swept and evicted above.
            await _delay(_erasureOptions.RevocationGrace);
            await ForEach(eventStores, failures, eventStore => Retry(() => keyStore.ConfirmErasureFor(eventStore, key.Namespace, identifier, requestId)));
            if (failures.Count == 0)
            {
                // Compaction is housekeeping: a failure cannot undo an acknowledged confirmation.
                var compactions = await Across(eventStores, async eventStore =>
                {
                    await keyStore.CompactErasureRequestsFor(eventStore, key.Namespace, identifier);
                    return true;
                });
                for (var deferred = compactions.Count(_ => _.Error is not null); deferred > 0; deferred--)
                {
                    // Leave the confirmed request inline; a future erasure retries archive-then-remove.
                    logger.CompactionDeferred();
                }
            }
        }
        CompleteErasure(identifier, key, eventStores, failures);
    }

    async Task DeleteLegacyEncryptionKey(EncryptionKeyIdentifier identifier, PIIManagerKey key, IReadOnlyList<EventStoreName> eventStores, List<Exception> failures)
    {
        // Release N retains the old provider contract. This path deliberately makes no protocol-confirmation claim.
        await ForEach(eventStores, failures, eventStore => keyStore.RecordErasureFor(eventStore, key.Namespace, identifier));
        await ForEach(eventStores, failures, eventStore => keyStore.DeleteFor(eventStore, key.Namespace, identifier));
        await ForEach(eventStores, failures, eventStore => RemoveUniqueClaimsFor(eventStore, key.Namespace, identifier));
        await ForEach(eventStores, failures, eventStore => cacheClient.Evict(eventStore, key.Namespace, identifier));
        CompleteErasure(identifier, key, eventStores, failures);
    }

    bool SupportsProtocolErasure() => keyStore.FenceGuarantees == (EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave);

    void CompleteErasure(EncryptionKeyIdentifier identifier, PIIManagerKey key, IReadOnlyList<EventStoreName> eventStores, List<Exception> failures)
    {
        if (failures.Count > 0)
        {
            logger.SubjectErasureIncomplete(BindingFor(identifier), key.Namespace, failures.Count, eventStores.Count, Names(eventStores));
            failures.ThrowIfUnderReview();
            throw new EncryptionKeyErasureIncomplete(identifier, failures);
        }

        logger.ErasedSubject(BindingFor(identifier), key.Namespace, eventStores.Count, Names(eventStores));
    }

    async Task AllowNewEncryptionKey(EncryptionKeyIdentifier identifier, PIIManagerKey key, IReadOnlyList<EventStoreName> eventStores, List<Exception> failures)
    {
        await ForEach(eventStores, failures, eventStore => SupportsProtocolErasure()
            ? keyStore.AuthorizeConfirmedSuccessorFor(eventStore, key.Namespace, identifier)
            : keyStore.AllowNewKeyFor(eventStore, key.Namespace, identifier));

        // A cached absence must not hide the authorization to provision a fresh key.
        await ForEach(eventStores, failures, eventStore => cacheClient.Evict(eventStore, key.Namespace, identifier));
        if (failures.Count > 0)
        {
            logger.NewEncryptionKeyAuthorizationIncomplete(BindingFor(identifier), key.Namespace, failures.Count, eventStores.Count, Names(eventStores));
            failures.ThrowIfUnderReview();
            throw new EncryptionKeyLifecycleIncomplete(identifier, failures);
        }

        logger.AllowedNewEncryptionKey(BindingFor(identifier), key.Namespace, eventStores.Count, Names(eventStores));
    }

    async Task<string?> PendingRequest(EncryptionKeyIdentifier identifier, PIIManagerKey key, IReadOnlyList<EventStoreName> eventStores, HashSet<EventStoreName> unreachable, List<Exception> failures)
    {
        var outcomes = await Across(eventStores, eventStore => keyStore.GetFenceFor(eventStore, key.Namespace, identifier));

        // Every store's fence is read concurrently, including stores after the one holding the pending request; that
        // extra reading is the accepted cost of not reaching the stores one at a time. Read in event store order,
        // stopping at the first pending request, so the request resumed is the same one a sequential lookup would find.
        foreach (var (eventStore, fence, error) in outcomes)
        {
            if (error is null)
            {
                if (fence?.Requests?.LastOrDefault(_ => !_.Confirmed) is { } pending) return pending.RequestId;
                continue;
            }

            if (IsUnavailable(error))
            {
                // The fence did not answer within its timeout; asking it to record the erasure would wait again.
                unreachable.Add(eventStore);
                failures.Add(error);
            }
            else
            {
                // The normal fan-out records the failure without preventing other stores being fenced.
                logger.RequestLookupDeferred();
            }
        }

        return null;
    }

    async Task Retry(Func<Task> operation)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception error) when (ShouldRetry(error, attempt))
            {
                await _delay(TimeSpan.FromMilliseconds(Math.Min(500, 10 * (1 << attempt)) + _random.Next(10)));
            }
        }
    }

    async Task<IReadOnlyList<EventStoreName>> EventStoresToReach(PIIManagerKey key, List<Exception> failures)
    {
        if (IsSystem(key.EventStore)) return [key.EventStore];
        try
        {
            // The event store this manager is addressed at is always included, even when the cluster listing does
            // not have it yet - an erasure that skipped the store the caller named would be the most surprising
            // outcome of all, and the listing is registration state rather than a statement about which keys exist.
            var eventStores = await storage.GetEventStores();
            return [.. eventStores.Append(key.EventStore).Where(eventStore => !IsSystem(eventStore)).Distinct()];
        }
        catch (Exception error)
        {
            // Losing the listing must not lose the erasure. The event store the caller named is erased either way,
            // and the failure is reported so a partial reach is never mistaken for a complete one.
            failures.Add(error);
            return [key.EventStore];
        }
    }

    async Task RemoveUniqueClaimsFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, EncryptionKeyIdentifier identifier)
    {
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        var subject = new Subject(identifier.Value);
        foreach (var eventSequence in await namespaceStorage.GetEventSequences())
        {
            await namespaceStorage.GetUniqueConstraintsStorage(eventSequence).RemoveForSubject(subject);
        }
    }

    PIIManagerKey GetKey()
    {
        _ = this.GetPrimaryKey(out var primaryKeyExtension);
        return (PIIManagerKey)primaryKeyExtension!;
    }
}
