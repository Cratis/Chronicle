// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;

namespace Cratis.Chronicle.Testing.Auditing;

/// <summary>
/// Represents a null implementation of <see cref="ICausationManager"/>.
/// </summary>
public class CausationManagerForTesting : ICausationManager
{
    static readonly Causation _root = new(DateTimeOffset.UtcNow, CausationType.Unknown, ImmutableDictionary<string, string>.Empty);

    /// <inheritdoc/>
    public Causation Root => _root;

    /// <inheritdoc/>
    public void Add(CausationType type, IDictionary<string, string> properties)
    {
    }

    /// <inheritdoc/>
    public IImmutableList<Causation> GetCurrentChain() => ImmutableList<Causation>.Empty;
}
