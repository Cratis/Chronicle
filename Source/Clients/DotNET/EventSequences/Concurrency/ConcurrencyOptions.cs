// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Represents options for configuring concurrency strategies.
/// </summary>
public class ConcurrencyOptions
{
    /// <summary>
    /// The value <see cref="CheckFirstAppendIntoAScope"/> takes when nothing configures it.
    /// </summary>
    /// <remarks>
    /// False keeps the behavior every released version has had: the first append into a narrowed scope is not
    /// checked. It is scheduled to become true in the next major version, and flipping this one value is the whole
    /// change - <see cref="OptimisticConcurrencyStrategy"/> and the options property both read it, so nothing else
    /// has to move.
    /// </remarks>
    public const bool CheckFirstAppendIntoAScopeByDefault = false;

    Type _defaultStrategy = typeof(OptimisticConcurrencyStrategy);

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
    /// Gets or sets a value indicating whether the first append into a concurrency scope is checked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A concurrency check needs something to compare against, and <see cref="OptimisticConcurrencyStrategy"/> gets
    /// it by reading the tail through the scope's own narrowing. When nothing matches that narrowing yet there is
    /// no tail to read. With this false - the default, and what every released version does - the scope carries no
    /// expectation and the kernel skips the check, so the first append into each scope goes through unchecked. That
    /// is the append most exposed to a race, because it opens a new narrowed partition on a stream other writers
    /// are already using.
    /// </para>
    /// <para>
    /// With this true the strategy says what a first append actually expects - that no event matching the narrowing
    /// exists - and the kernel checks it, rejecting the append with a concurrency violation if one appeared in the
    /// meantime. <strong>Turning it on will reject appends that succeed with it off</strong>, which is why it is
    /// opt-in rather than the default. It is scheduled to become the default in the next major version.
    /// </para>
    /// <para>
    /// This governs the strategy. A single append can ask for the same check without turning this on, by building a
    /// scope with <see cref="ConcurrencyScopeBuilder.ExpectingNoMatchingEvent"/>.
    /// </para>
    /// </remarks>
    public bool CheckFirstAppendIntoAScope { get; set; } = CheckFirstAppendIntoAScopeByDefault;
}
