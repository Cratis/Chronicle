// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Assertions;

namespace Cratis.Chronicle.Testing.ReadModels;

public partial class ReadModelScenario<TReadModel>
    where TReadModel : class
{
    /// <summary>
    /// Recursively asserts the listed public properties at the specified materialized read-model key.
    /// </summary>
    /// <param name="eventSourceId">The read-model key, which need not be the originating event source.</param>
    /// <param name="expected">The partial shape; unlisted properties are ignored.</param>
    /// <exception cref="ReadModelAssertionFailed">Keyed state is unavailable, the instance is absent, or a listed value differs.</exception>
    public void ShouldPartiallyMatchReadModel(EventSourceId eventSourceId, object expected) => AssertMatch(eventSourceId, expected, ValueComparison.Partial);

    /// <summary>
    /// Recursively asserts the complete public readable property set at the materialized read-model key.
    /// </summary>
    /// <param name="eventSourceId">The read-model key.</param>
    /// <param name="expected">The complete shape, which may use different object types but retains scalar types and collection cardinality.</param>
    /// <exception cref="ReadModelAssertionFailed">Keyed state is unavailable, the instance is absent, or its complete shape differs.</exception>
    public void ShouldExactlyMatchReadModel(EventSourceId eventSourceId, object expected) => AssertMatch(eventSourceId, expected, ValueComparison.Exact);

    /// <summary>
    /// Asserts that no instance exists at the specified materialized read-model key.
    /// </summary>
    /// <param name="eventSourceId">The read-model key.</param>
    /// <exception cref="ReadModelAssertionFailed">Keyed state is unavailable or an instance exists at this key.</exception>
    public void ShouldHaveNoReadModel(EventSourceId eventSourceId)
    {
        if (AssertionInstance(eventSourceId) is not null)
        {
            throw new ReadModelAssertionFailed($"Expected read model '{typeof(TReadModel).Name}' at key '{eventSourceId.Value}' not to exist, but an instance exists.");
        }
    }

    TReadModel? AssertionInstance(EventSourceId key)
    {
        var instance = InstanceForEventSourceId(key);
        if (_instance is not null && _instances.Count == 0)
        {
            throw new ReadModelAssertionFailed(
                $"Cannot assert read model '{typeof(TReadModel).Name}' at key '{key.Value}': materialized state exists but keyed state is unavailable. Use per-key seeds or existing appended-event folding for multi-source reducers.");
        }

        return instance;
    }

    void AssertMatch(EventSourceId key, object expected, ValueComparison mode)
    {
        var instance = AssertionInstance(key);
        var subject = $"read model '{typeof(TReadModel).Name}' at key '{key.Value}'";
        if (instance is null) throw new ReadModelAssertionFailed($"Expected {subject} to exist, but no instance was materialized.");
        var difference = ExpectedValueComparer.Compare(expected, instance, mode);
        if (difference is not null) throw new ReadModelAssertionFailed($"Expected {subject} to match{(mode == ValueComparison.Exact ? " exactly" : string.Empty)}, but {difference}.");
    }
}
