// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Represents options for configuring concurrency strategies.
/// </summary>
public class ConcurrencyOptions
{
    Type _defaultStrategy = typeof(OptimisticConcurrencyStrategy);
    private Type _aggregateRootStrategy = typeof(OptimisticConcurrencyStrategy);

    /// <summary>
    /// Gets or sets the default concurrency strategy to use when no specific strategy is provided.
    /// </summary>
    public Type DefaultStrategy
    {
        get => _defaultStrategy;
        set
        {
            TypeIsNotAConcurrencyStrategy.ThrowIfNotAConcurrencyStrategy(value);
            _defaultStrategy = value;
        }
    }

    /// <summary>
    /// Gets or sets the aggregate root strategy to use for concurrency control on aggregate roots.
    /// </summary>
    public Type AggregateRootStrategy
    {
        get => _aggregateRootStrategy;
        set
        {
            TypeIsNotAConcurrencyStrategy.ThrowIfNotAConcurrencyStrategy(value);
            _aggregateRootStrategy = value;
        }
    }
}
