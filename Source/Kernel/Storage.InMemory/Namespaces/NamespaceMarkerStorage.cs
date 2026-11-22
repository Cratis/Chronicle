// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.InMemory.Namespaces;

/// <summary>
/// Represents the namespace-wide marker and the shared append/close critical section.
/// </summary>
public sealed class NamespaceMarkerStorage : INamespaceMarkerStorage
{
    NamespaceMarker? _marker;

    /// <summary>Gets the critical section shared with every sequence in this namespace.</summary>
    internal Lock Gate { get; } = new();

    /// <inheritdoc/>
    public Task<NamespaceMarker?> Get()
    {
        lock (Gate)
        {
            return Task.FromResult(_marker);
        }
    }

    /// <inheritdoc/>
    public Task<bool> TryOpen(NamespaceIncarnationId incarnation, NamespaceIncarnationId? previous)
    {
        lock (Gate)
        {
            if (incarnation == NamespaceIncarnationId.NotSet) return Task.FromResult(false);
            if (_marker is not null && _marker.Incarnation == incarnation) return Task.FromResult(_marker.IsOpen);
            if (_marker is not null && (_marker.IsOpen || _marker.Incarnation != previous)) return Task.FromResult(false);
            _marker = new(incarnation, true, 0);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<bool> TryClose(NamespaceIncarnationId incarnation)
    {
        lock (Gate)
        {
            if (_marker is null || _marker.Incarnation != incarnation) return Task.FromResult(false);
            _marker = _marker with { IsOpen = false };
            return Task.FromResult(true);
        }
    }

    /// <summary>Tests and touches admission while the caller holds <see cref="Gate"/> through its inserts.</summary>
    /// <param name="incarnation">The captured incarnation.</param>
    /// <returns>Whether the captured identity still owns an open marker.</returns>
    internal bool TryAdmit(NamespaceIncarnationId incarnation)
    {
        if (_marker is not { IsOpen: true } || _marker.Incarnation != incarnation) return false;
        _marker = _marker with { Touch = _marker.Touch + 1 };
        return true;
    }
}
