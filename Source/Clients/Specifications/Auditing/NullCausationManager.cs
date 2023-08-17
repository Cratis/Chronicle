// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Auditing;

namespace Aksio.Cratis.Specifications.Auditing;

/// <summary>
/// Represents a null implementation of <see cref="ICausationManager"/>.
/// </summary>
public class NullCausationManager : ICausationManager
{
    static Causation _root = new(DateTimeOffset.UtcNow, CausationType.Unknown, ImmutableDictionary<string, string>.Empty);

    /// <inheritdoc/>
    public Causation Root => _root;

    /// <inheritdoc/>
    public void Add(CausationType Type, IDictionary<string, string> properties) { }

    /// <inheritdoc/>
    public IImmutableList<Causation> GetCurrentChain() => ImmutableList<Causation>.Empty;
}
