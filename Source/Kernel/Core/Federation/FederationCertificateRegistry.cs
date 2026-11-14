// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationCertificateRegistry"/> that folds the enrollment and certificate events in the system event log.
/// </summary>
/// <remarks>
/// Every read catches up from the last sequence number seen, so every silo converges on the same state. Mutations are
/// serialized by the <see cref="IFederationAuthority"/> grain, so there is only one decision maker in the cluster.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> to read the system event log from.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the event log to append to.</param>
/// <param name="serializer">The <see cref="IEventSerializer"/> to deserialize events with.</param>
[Singleton]
public class FederationCertificateRegistry(
    IStorage storage,
    IGrainFactory grainFactory,
    IEventSerializer serializer) : IFederationCertificateRegistry, IDisposable
{
    static readonly EventType[] _eventTypes =
    [
        typeof(FederationAuthorityCreated).GetEventType(),
        typeof(FederationEnrollmentCreated).GetEventType(),
        typeof(FederationEnrollmentRevoked).GetEventType(),
        typeof(FederationCertificateIssued).GetEventType(),
        typeof(FederationCertificateRevoked).GetEventType(),
        typeof(FederationIdentityInstalled).GetEventType(),
        typeof(FederationPolicySet).GetEventType(),
        typeof(FederationPolicySuspended).GetEventType(),
        typeof(FederationPolicyResumed).GetEventType(),
        typeof(FederationPolicyRevoked).GetEventType()
    ];

    readonly SemaphoreSlim _lock = new(1, 1);
    readonly FederationCertificateHistory _history = new();
    EventSequenceNumber _next = EventSequenceNumber.First;

    IEventSequenceStorage Events => storage.GetEventStore(EventStoreName.System)
        .GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.Log);

    /// <inheritdoc/>
    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<FederationEnrollmentState>> GetEnrollments() => Read(_ => _.Enrollments);

    /// <inheritdoc/>
    public Task<FederationEnrollmentState?> FindEnrollment(string id) => Read(_ => _.FindEnrollment(id));

    /// <inheritdoc/>
    public Task<IReadOnlyList<FederationIssuedCertificateState>> GetIssuedCertificates() => Read(_ => _.Certificates);

    /// <inheritdoc/>
    public Task<FederationIssuedCertificateState?> FindIssuedCertificate(string serial) => Read(_ => _.FindCertificate(serial));

    /// <inheritdoc/>
    public Task<IReadOnlyList<FederationPolicyState>> GetPolicies() => Read(_ => _.Policies);

    /// <inheritdoc/>
    public Task<FederationPolicyState?> FindPolicy(string subject) => Read(_ => _.FindPolicy(subject));

    /// <inheritdoc/>
    public Task<FederationAuthorityCreated?> GetAuthority() => Read(_ => _.Authority);

    /// <inheritdoc/>
    public Task<FederationIdentityInstalled?> GetInstalledIdentity() => Read(_ => _.Identity);

    /// <inheritdoc/>
    public async Task Append(string eventSourceId, object @event)
    {
        var result = await grainFactory.GetEventLog().Append(eventSourceId, @event);
        if (!result.IsSuccess)
        {
            throw new FederationEventAppendFailed(eventSourceId, string.Join("; ", result.Errors));
        }
    }

    async Task<TResult> Read<TResult>(Func<FederationCertificateHistory, TResult> select)
    {
        await _lock.WaitAsync();
        try
        {
            await CatchUp();
            return select(_history);
        }
        finally
        {
            _lock.Release();
        }
    }

    async Task CatchUp()
    {
        var tail = await Events.GetTailSequenceNumber(_eventTypes);
        if (!tail.IsActualValue || tail < _next)
        {
            return;
        }

        using var cursor = await Events.GetRange(_next, tail, eventTypes: _eventTypes);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                _history.Apply(@event.Context.EventSourceId.Value, serializer.Deserialize(@event));
            }
        }

        _next = tail.Next();
    }
}
