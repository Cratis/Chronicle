// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationRegistry"/> that folds the federation events in the system event log.
/// </summary>
/// <remarks>
/// Every read catches up from the last sequence number seen, so every silo converges on the same state. Mutations append
/// events to the system event log, which is the audit trail; the append path records the identity and causation.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> to read the system event log from.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the event log to append to.</param>
/// <param name="serializer">The <see cref="IEventSerializer"/> to deserialize events with.</param>
/// <param name="options">The <see cref="ChronicleOptions"/> holding the federation configuration.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class FederationRegistry(
    IStorage storage,
    IGrainFactory grainFactory,
    IEventSerializer serializer,
    IOptions<ChronicleOptions> options,
    ILogger<FederationRegistry> logger) : IFederationRegistry, IDisposable
{
    static readonly EventType[] _eventTypes =
    [
        typeof(FederationPeerBound).GetEventType(),
        typeof(FederationPeerUnbound).GetEventType(),
        typeof(FederationGrantSet).GetEventType(),
        typeof(FederationLinkRevoked).GetEventType()
    ];

    readonly SemaphoreSlim _lock = new(1, 1);
    readonly FederationHistory _history = new();
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
    public Task<IReadOnlyList<FederationBindingState>> GetBindings() => Read(_ => _.Bindings);

    /// <inheritdoc/>
    public Task<IReadOnlyList<FederationGrantState>> GetGrants() => Read(_ => _.Grants);

    /// <inheritdoc/>
    public Task<IReadOnlyList<FederationGrantState>> FindActiveGrants(CertificatePin pin) =>
        Read<IReadOnlyList<FederationGrantState>>(history => [.. history.Grants.Where(_ => !_.Revoked && Matches(_.ClientPin, pin))]);

    /// <inheritdoc/>
    public async Task Bind(FederationBindingState binding)
    {
        var @event = ToBoundEvent(binding, FederationOrigin.Runtime);
        await Mutate(binding.Id, @event, _ => { });
    }

    /// <inheritdoc/>
    public Task Unbind(string id) => Mutate(
        id,
        new FederationPeerUnbound(),
        history =>
        {
            if (!history.HasBinding(id))
            {
                throw new FederationBindingNotFound(id);
            }
        });

    /// <inheritdoc/>
    public async Task SetGrant(FederationGrantState grant)
    {
        var @event = ToGrantEvent(grant, FederationOrigin.Runtime);
        await Mutate(grant.Id, @event, _ => { });
    }

    /// <inheritdoc/>
    public Task Revoke(string id) => Mutate(
        id,
        new FederationLinkRevoked(),
        history =>
        {
            if (!history.HasGrant(id))
            {
                throw new FederationLinkNotFound(id);
            }
        });

    /// <inheritdoc/>
    public async Task BootstrapFromConfiguration()
    {
        var federation = options.Value.Federation;
        if (federation.Bindings.Count == 0 && federation.Grants.Count == 0)
        {
            return;
        }

        await _lock.WaitAsync();
        try
        {
            await CatchUp();

            foreach (var (id, configured) in federation.Bindings)
            {
                if (_history.HasBindingHistory(id))
                {
                    continue;
                }

                try
                {
                    var state = new FederationBindingState(
                        id,
                        configured.LogicalSource,
                        configured.Endpoint,
                        configured.ServerPin,
                        configured.ClientCertificate,
                        configured.RemoteEventStore,
                        configured.RemoteNamespace,
                        configured.TargetEventStore,
                        configured.TargetNamespace,
                        FederationOrigin.Configuration);
                    await Append(id, ToBoundEvent(state, FederationOrigin.Configuration));
                    logger.BootstrappedBinding(id);
                }
                catch (InvalidFederationBinding ex)
                {
                    logger.SkippingInvalidConfiguredBinding(id, ex.Message);
                }
            }

            foreach (var (id, configured) in federation.Grants)
            {
                if (_history.HasGrantHistory(id))
                {
                    continue;
                }

                try
                {
                    var state = new FederationGrantState(
                        id,
                        configured.ClientPin,
                        configured.EventStore,
                        [.. configured.Namespaces],
                        configured.AllPublic,
                        [.. configured.EventTypes],
                        false,
                        FederationOrigin.Configuration);
                    await Append(id, ToGrantEvent(state, FederationOrigin.Configuration));
                    logger.BootstrappedGrant(id);
                }
                catch (InvalidFederationGrant ex)
                {
                    logger.SkippingInvalidConfiguredGrant(id, ex.Message);
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    static bool Matches(string clientPin, CertificatePin pin)
    {
        try
        {
            return CertificatePin.Parse(clientPin).Equals(pin);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    static string NormalizePin(string value, Func<string, Exception> invalid)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw invalid("a certificate pin is required");
        }

        try
        {
            return CertificatePin.Parse(value).ToString();
        }
        catch (FormatException)
        {
            throw invalid("a certificate pin must be 'sha256:<base64>' of a SHA-256 hash");
        }
    }

    static FederationPeerBound ToBoundEvent(FederationBindingState binding, string origin)
    {
        InvalidFederationBinding Invalid(string reason) => new(binding.Id, reason);

        if (string.IsNullOrWhiteSpace(binding.Id))
        {
            throw Invalid("an id is required");
        }

        Require(binding.LogicalSource, "a logical source is required", Invalid);
        Require(binding.ClientCertificate, "a client certificate name is required", Invalid);
        Require(binding.RemoteEventStore, "a remote event store is required", Invalid);
        Require(binding.RemoteNamespace, "a remote namespace is required", Invalid);
        Require(binding.TargetEventStore, "a target event store is required", Invalid);
        Require(binding.TargetNamespace, "a target namespace is required", Invalid);

        if (!Uri.TryCreate(binding.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw Invalid("the endpoint must be an absolute https URI");
        }

        return new(
            binding.LogicalSource,
            binding.Endpoint,
            NormalizePin(binding.ServerPin, Invalid),
            binding.ClientCertificate,
            binding.RemoteEventStore,
            binding.RemoteNamespace,
            binding.TargetEventStore,
            binding.TargetNamespace,
            origin);
    }

    static FederationGrantSet ToGrantEvent(FederationGrantState grant, string origin)
    {
        InvalidFederationGrant Invalid(string reason) => new(grant.Id, reason);

        if (string.IsNullOrWhiteSpace(grant.Id))
        {
            throw Invalid("an id is required");
        }

        Require(grant.EventStore, "an event store is required", Invalid);

        var namespaces = grant.Namespaces.ToArray();
        if (namespaces.Length == 0 || namespaces.Any(string.IsNullOrWhiteSpace))
        {
            throw Invalid("at least one namespace is required and none may be empty");
        }

        var eventTypes = grant.AllPublic ? [] : grant.EventTypes.ToArray();
        if (!grant.AllPublic && (eventTypes.Length == 0 || eventTypes.Any(string.IsNullOrWhiteSpace)))
        {
            throw Invalid("a grant that is not for all public event types needs at least one event type and none may be empty");
        }

        return new(NormalizePin(grant.ClientPin, Invalid), grant.EventStore, namespaces, grant.AllPublic, eventTypes, origin);
    }

    static void Require(string value, string reason, Func<string, Exception> invalid)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw invalid(reason);
        }
    }

    async Task<TResult> Read<TResult>(Func<FederationHistory, TResult> select)
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

    async Task Mutate(string id, object @event, Action<FederationHistory> validateAgainstState)
    {
        await _lock.WaitAsync();
        try
        {
            await CatchUp();
            validateAgainstState(_history);
            await Append(id, @event);
        }
        finally
        {
            _lock.Release();
        }
    }

    async Task Append(string id, object @event)
    {
        var result = await grainFactory.GetEventLog().Append(id, @event);
        if (!result.IsSuccess)
        {
            throw new FederationEventAppendFailed(id, string.Join("; ", result.Errors));
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
