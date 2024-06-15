// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Auditing;

/// <summary>
/// Defines a system that manages causation.
/// </summary>
public interface ICausationManager
{
    /// <summary>
    /// Gets the root causation.
    /// </summary>
    Causation Root { get; }

    /// <summary>
    /// Gets the current causation.
    /// </summary>
    /// <returns>A collection of <see cref="Causation"/>.</returns>
    IImmutableList<Causation> GetCurrentChain();

    /// <summary>
    /// Adds a causation.
    /// </summary>
    /// <param name="type">Type to add.</param>
    /// <param name="properties">Properties associated with the causation.</param>
    void Add(CausationType type, IDictionary<string, string> properties);
}
