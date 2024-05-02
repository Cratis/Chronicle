// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Auditing;

/// <summary>
/// Represents an implementation of <see cref="ICausationManager"/>.
/// </summary>
public class CausationManager : ICausationManager
{
    readonly AsyncLocal<List<Causation>> _current = new();

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

    /// <summary>
    /// Defines the root causation for the current process.
    /// </summary>
    /// <param name="properties">Properties associated with the root causation.</param>
    internal void DefineRoot(IDictionary<string, string> properties)
    {
        Root = new Causation(DateTimeOffset.UtcNow, CausationType.Root, properties.ToImmutableDictionary());
    }
}
