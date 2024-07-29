// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRoot"/> for testing.
/// </summary>
public class AggregateRootMutatorForTesting : IAggregateRootMutator
{
    /// <inheritdoc/>
    public Task Dehydrate() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Mutate(object @event) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Rehydrate() => Task.CompletedTask;
}
