// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Auditing;

/// <summary>
/// Represents an implementation of <see cref="ICausationManager"/>.
/// </summary>
public class CausationManager : ICausationManager
{
    static readonly AsyncLocal<List<Causation>> _current = new();

    /// <inheritdoc/>
    public Causation Root { get; private set; } = new(DateTimeOffset.UtcNow, CausationType.Unknown, ImmutableDictionary<string, string>.Empty);

    /// <inheritdoc/>
    public IImmutableList<Causation> GetCurrentChain()
    {
        _current.Value ??= [];
        if (_current.Value.Count == 0)
        {
            _current.Value.Add(Root);
        }

        return _current.Value.ToImmutableList();
    }

    /// <inheritdoc/>
    public void Add(CausationType type, IDictionary<string, string> properties)
    {
        _current.Value ??= [];
        if (_current.Value.Count == 0)
        {
            _current.Value.Add(Root);
        }

        _current.Value.Add(new Causation(DateTimeOffset.UtcNow, type, properties.ToImmutableDictionary()));
    }

    /// <inheritdoc/>
    public IDisposable BeginScope(CausationType type, IDictionary<string, string> properties)
    {
        Add(type, properties);
        var chain = _current.Value!;
        return new Scope(chain, chain.Count - 1);
    }

    /// <summary>
    /// Defines the root causation for the current process.
    /// </summary>
    /// <param name="properties">Properties associated with the root causation.</param>
    internal void DefineRoot(IDictionary<string, string> properties)
    {
        Root = new Causation(DateTimeOffset.UtcNow, CausationType.Root, properties.ToImmutableDictionary());
    }

    /// <summary>
    /// Removes a causation and everything added after it when disposed.
    /// </summary>
    /// <param name="chain">The chain the causation was added to.</param>
    /// <param name="index">The position the causation was added at.</param>
    /// <remarks>
    /// The chain is held by reference rather than read back off the ambient value, because the scope can be
    /// disposed from a different async branch than the one it was created on - which is exactly what happens when a
    /// command completes after awaiting. Truncating removes anything added after the causation too, which is what
    /// last-in first-out means and is the only interpretation that leaves the chain consistent when scopes are
    /// disposed out of order.
    /// </remarks>
    sealed class Scope(List<Causation> chain, int index) : IDisposable
    {
        bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (index < chain.Count)
            {
                chain.RemoveRange(index, chain.Count - index);
            }
        }
    }
}
