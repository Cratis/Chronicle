// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Auditing;

/// <summary>
/// Represents an implementation of <see cref="ICausationManager"/>.
/// </summary>
public class CausationManager : ICausationManager
{
    static readonly AsyncLocal<List<Entry>> _current = new();

    /// <inheritdoc/>
    public Causation Root { get; private set; } = new(DateTimeOffset.UtcNow, CausationType.Unknown, ImmutableDictionary<string, string>.Empty);

    /// <inheritdoc/>
    public IImmutableList<Causation> GetCurrentChain() => GetCurrentEntries().Select(_ => _.Causation).ToImmutableList();

    /// <inheritdoc/>
    public void Add(CausationType type, IDictionary<string, string> properties)
    {
        _current.Value = [.. GetCurrentEntries(), new(new Causation(DateTimeOffset.UtcNow, type, properties.ToImmutableDictionary()))];
    }

    /// <inheritdoc/>
    public IDisposable BeginScope(CausationType type, IDictionary<string, string> properties)
    {
        Add(type, properties);

        return new Scope(_current.Value![^1]);
    }

    /// <summary>
    /// Defines the root causation for the current process.
    /// </summary>
    /// <param name="properties">Properties associated with the root causation.</param>
    internal void DefineRoot(IDictionary<string, string> properties)
    {
        Root = new Causation(DateTimeOffset.UtcNow, CausationType.Root, properties.ToImmutableDictionary());
    }

    List<Entry> GetCurrentEntries()
    {
        _current.Value ??= [];
        var disposedIndex = _current.Value.FindIndex(_ => _.IsDisposed);
        if (disposedIndex >= 0)
        {
            _current.Value = _current.Value.Take(disposedIndex).ToList();
        }

        if (_current.Value.Count == 0)
        {
            _current.Value = [new(Root)];
        }

        return _current.Value;
    }

    /// <summary>
    /// Represents a causation whose lifetime can be bounded by a scope.
    /// </summary>
    /// <param name="causation">The causation represented by the entry.</param>
    /// <remarks>
    /// Only the scope lifetime is shared across async branches, so disposal after awaiting still removes the
    /// scoped causation. Each flow truncates its own chain at the first disposed entry without mutating a list
    /// inherited by another flow.
    /// </remarks>
    sealed class Entry(Causation causation)
    {
        volatile bool _disposed;

        /// <summary>
        /// Gets the causation represented by the entry.
        /// </summary>
        public Causation Causation { get; } = causation;

        /// <summary>
        /// Gets whether the scope has been disposed.
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Ends the scope represented by the entry.
        /// </summary>
        public void EndScope() => _disposed = true;
    }

    /// <summary>
    /// Represents the lifetime of a scoped causation.
    /// </summary>
    /// <param name="entry">The entry whose scope ends on disposal.</param>
    sealed class Scope(Entry entry) : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose() => entry.EndScope();
    }
}
