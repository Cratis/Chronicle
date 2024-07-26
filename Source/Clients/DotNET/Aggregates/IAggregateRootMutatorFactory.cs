// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Defines a system that can create instances of <see cref="IAggregateRoot"/>.
/// </summary>
public interface IAggregateRootMutatorFactory
{
    /// <summary>
    /// Create an instance of <see cref="IAggregateRootMutator"/> for the specified <typeparamref name="TState"/>.
    /// </summary>
    /// <typeparam name="TState">The state type to create a mutator for.</typeparam>
    /// <param name="context">The <see cref="IAggregateRootContext"/> to create the mutator for.</param>
    /// <returns><see cref="IAggregateRootMutator"/> created.</returns>
    Task<IAggregateRootMutator> Create<TState>(IAggregateRootContext context);
}
