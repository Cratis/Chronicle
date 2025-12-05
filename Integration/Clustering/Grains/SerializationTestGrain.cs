// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Clustering.Integration.Grains;

/// <summary>
/// Grain implementation for testing serialization of various types across the cluster.
/// </summary>
public class SerializationTestGrain : Grain, ISerializationTestGrain
{
    /// <inheritdoc/>
    public Task<EventStoreName> TestConceptAs(EventStoreName eventStoreName) => Task.FromResult(eventStoreName);

    /// <inheritdoc/>
    public Task<IEnumerable<string>> TestIEnumerable(IEnumerable<string> items) => Task.FromResult(items);

    /// <inheritdoc/>
    public Task<IImmutableList<string>> TestIImmutableList(IImmutableList<string> items) => Task.FromResult(items);

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> TestIReadOnlyList(IReadOnlyList<string> items) => Task.FromResult(items);

    /// <inheritdoc/>
    public Task<IEnumerable<EventStoreName>> TestIEnumerableOfConcept(IEnumerable<EventStoreName> items) => Task.FromResult(items);

    /// <inheritdoc/>
    public Task<IImmutableList<EventStoreName>> TestIImmutableListOfConcept(IImmutableList<EventStoreName> items) => Task.FromResult(items);

    /// <inheritdoc/>
    public Task<IReadOnlyList<EventStoreName>> TestIReadOnlyListOfConcept(IReadOnlyList<EventStoreName> items) => Task.FromResult(items);
}
