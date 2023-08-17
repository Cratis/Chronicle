// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Auditing;

public class CausationManager : ICausationManager
{
    static Causation _root = new(DateTimeOffset.UtcNow, CausationType.Unknown, ImmutableDictionary<string, string>.Empty);

    AsyncLocal<List<Causation>> _current = new();

    /// <inheritdoc/>
    public Causation Root => _root;

    /// <inheritdoc/>
    public IImmutableList<Causation> GetCurrentChain()
    {
        _current.Value ??= new();
        if (_current.Value.Count == 0)
        {
            _current.Value.Add(_root);
        }

        return _current.Value.ToImmutableList();
    }

    /// <inheritdoc/>
    public void Add(CausationType Type, IDictionary<string, string> properties)
    {
        _current.Value ??= new();
        if (_current.Value.Count == 0)
        {
            _current.Value.Add(_root);
        }

        _current.Value.Add(new Causation(DateTimeOffset.UtcNow, Type, properties.ToImmutableDictionary()));
    }

    /// <summary>
    /// Defines the root causation for the current process.
    /// </summary>
    /// <param name="properties">Properties associated with the root causation.</param>
    internal static void DefineRoot(IDictionary<string, string> properties)
    {
        _root = new Causation(DateTimeOffset.UtcNow, CausationType.Root, properties.ToImmutableDictionary());
    }
}
