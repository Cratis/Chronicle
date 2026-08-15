// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_OptimisticConcurrencyStrategy.given;

/// <summary>
/// The same strategy with the first-append check turned on, which is the only thing that separates the two
/// behaviors. Building it through <see cref="ConcurrencyOptions"/> rather than a second strategy type is what makes
/// flipping the default in a later major a one-value change.
/// </summary>
public class an_optimistic_concurrency_strategy_that_checks_the_first_append : an_optimistic_concurrency_strategy
{
    void Establish() => _strategy = new OptimisticConcurrencyStrategy(
        _eventSequence,
        new ConcurrencyOptions { CheckFirstAppendIntoAScope = true });
}
