// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Clustering.Integration.Grains;

/// <summary>
/// Grain interface for testing serialization of various types across the cluster.
/// </summary>
public interface ISerializationTestGrain : IGrainWithStringKey
{
    /// <summary>
    /// Test serialization of a ConceptAs type.
    /// </summary>
    /// <param name="eventStoreName">Event store name to test.</param>
    /// <returns>The same value received.</returns>
    Task<EventStoreName> TestConceptAs(EventStoreName eventStoreName);

    /// <summary>
    /// Test serialization of IEnumerable.
    /// </summary>
    /// <param name="items">Collection of strings.</param>
    /// <returns>The same collection received.</returns>
    Task<IEnumerable<string>> TestIEnumerable(IEnumerable<string> items);

    /// <summary>
    /// Test serialization of IImmutableList.
    /// </summary>
    /// <param name="items">Immutable list of strings.</param>
    /// <returns>The same list received.</returns>
    Task<IImmutableList<string>> TestIImmutableList(IImmutableList<string> items);

    /// <summary>
    /// Test serialization of IReadOnlyList.
    /// </summary>
    /// <param name="items">Read only list of strings.</param>
    /// <returns>The same list received.</returns>
    Task<IReadOnlyList<string>> TestIReadOnlyList(IReadOnlyList<string> items);

    /// <summary>
    /// Test serialization of IEnumerable with ConceptAs types.
    /// </summary>
    /// <param name="items">Collection of EventStoreName.</param>
    /// <returns>The same collection received.</returns>
    Task<IEnumerable<EventStoreName>> TestIEnumerableOfConcept(IEnumerable<EventStoreName> items);

    /// <summary>
    /// Test serialization of IImmutableList with ConceptAs types.
    /// </summary>
    /// <param name="items">Immutable list of EventStoreName.</param>
    /// <returns>The same list received.</returns>
    Task<IImmutableList<EventStoreName>> TestIImmutableListOfConcept(IImmutableList<EventStoreName> items);

    /// <summary>
    /// Test serialization of IReadOnlyList with ConceptAs types.
    /// </summary>
    /// <param name="items">Read only list of EventStoreName.</param>
    /// <returns>The same list received.</returns>
    Task<IReadOnlyList<EventStoreName>> TestIReadOnlyListOfConcept(IReadOnlyList<EventStoreName> items);
}
