// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Tracks connection claims on this silo.
/// </summary>
[Singleton]
public class ConnectionOwnership : IConnectionOwnership
{
    readonly ConcurrentDictionary<ConnectionId, ConnectionClaim> _claims = new();
    readonly ConcurrentDictionary<(ConnectionId, object), object> _subscriptions = new();
    readonly SemaphoreSlim[] _gates = Enumerable.Range(0, 256).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    /// <inheritdoc/>
    public async ValueTask<IDisposable> Enter(ConnectionId id, CancellationToken cancellationToken = default)
    {
        var gate = _gates[(id.GetHashCode() & int.MaxValue) % _gates.Length];
        await gate.WaitAsync(cancellationToken);
        return new GateLease(gate);
    }

    /// <inheritdoc/>
    public object Subscribe(ConnectionId id, object key)
    {
        var token = new object();
        _subscriptions[(id, key)] = token;
        return token;
    }

    /// <inheritdoc/>
    public bool IsCurrentSubscription(ConnectionId id, object key, object token) =>
        _subscriptions.TryGetValue((id, key), out var current) && ReferenceEquals(current, token);

    /// <inheritdoc/>
    public void ReleaseSubscription(ConnectionId id, object key, object token) =>
        ((ICollection<KeyValuePair<(ConnectionId, object), object>>)_subscriptions).Remove(new((id, key), token));

    /// <inheritdoc/>
    public ConnectionClaim? Claim(ConnectionId id, ConnectionOwner owner)
    {
        if (owner == ConnectionOwner.Unidentified) return null;
        var claim = new ConnectionClaim(owner);
        while (true)
        {
            if (!_claims.TryGetValue(id, out var current))
            {
                if (_claims.TryAdd(id, claim)) return claim;
                continue;
            }

            if (current.Owner != owner) return null;
            if (_claims.TryUpdate(id, claim, current)) return claim;
        }
    }

    /// <inheritdoc/>
    public ConnectionAccess Authorize(ConnectionId id, ConnectionOwner owner)
    {
        if (owner == ConnectionOwner.Unidentified) return ConnectionAccess.Denied;
        if (_claims.TryGetValue(id, out var claim)) return claim.Owner == owner ? ConnectionAccess.Allowed : ConnectionAccess.Denied;

        return owner == ConnectionOwner.Anonymous || owner == ConnectionOwner.InProcess ? ConnectionAccess.Allowed : ConnectionAccess.Unknown;
    }

    /// <inheritdoc/>
    public bool IsOwnedBy(ConnectionId id, ConnectionOwner owner) => _claims.TryGetValue(id, out var claim) && claim.Owner == owner;

    /// <inheritdoc/>
    public ConnectionClaim? GetClaim(ConnectionId id) => _claims.GetValueOrDefault(id);

    /// <inheritdoc/>
    public bool IsCurrent(ConnectionId id, ConnectionClaim claim) => _claims.TryGetValue(id, out var current) && ReferenceEquals(current, claim);

    /// <inheritdoc/>
    public bool Release(ConnectionId id, ConnectionClaim claim) => ((ICollection<KeyValuePair<ConnectionId, ConnectionClaim>>)_claims).Remove(new(id, claim));

    sealed class GateLease(SemaphoreSlim gate) : IDisposable
    {
        SemaphoreSlim? _gate = gate;

        public void Dispose() => Interlocked.Exchange(ref _gate, null)?.Release();
    }
}
