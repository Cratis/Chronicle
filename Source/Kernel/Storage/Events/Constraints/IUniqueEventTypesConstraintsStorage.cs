// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Defines the storage mechanism for unique event type constraints.
/// </summary>
public interface IUniqueEventTypesConstraintsStorage
{
    /// <summary>
    /// Check if a constraint value exists.
    /// </summary>
    /// <param name="definition">The <see cref="UniqueEventTypeConstraintDefinition"/> to check against.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to check.</param>
    /// <param name="scopeKey">Optional scope key for scoped constraints.</param>
    /// <returns>
    /// Tuple containing a boolean saying whether or not its allowed to perform and the <see cref="EventSequenceNumber"/> for the item it violates.
    /// Returns <see cref="EventSequenceNumber.Unavailable"/> if it doesn't exist.
    /// </returns>
    /// <remarks>
    /// An append is allowed only when the event source carries none of the covered event types. A definition covering
    /// more than one makes them mutually exclusive: the first of them to be appended blocks all of the others.
    /// <para>
    /// A definition carrying a <see cref="UniqueEventTypeConstraintDefinition.RemovedWith"/> is answered per cycle
    /// rather than forever — only a covered event appended after the most recent removal event on that event source
    /// blocks the append, so an event source that has been released is free to start the next cycle. The whole
    /// definition is passed rather than its parts so that a reader cannot answer against half of it.
    /// </para>
    /// <para>
    /// A definition may declare several removal events, because a cycle can end in more than one way. The cycle
    /// ends at the most recent of them, not at the most recent of any one of them.
    /// </para>
    /// <para>
    /// This is the original member and stays the only abstract one, so an existing provider compiled against it
    /// keeps working unchanged. It narrows by a flattened key, which cannot tell two scopes apart whose dimension
    /// values contain the characters the key joins on. New work must also implement this member — it remains
    /// abstract — but should also implement <see cref="IsAllowedWithinScope"/>, which receives the dimensions as
    /// typed values; a provider that does so may leave this member unsupported for anything but an unscoped lookup.
    /// </para>
    /// </remarks>
    Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowed(UniqueEventTypeConstraintDefinition definition, EventSourceId eventSourceId, string scopeKey = "");

    /// <summary>
    /// Check if a constraint value exists within the scope of the event being validated.
    /// </summary>
    /// <param name="definition">The <see cref="UniqueEventTypeConstraintDefinition"/> to check against.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to check.</param>
    /// <param name="scope">Optional <see cref="ResolvedConstraintScope"/> narrowing the lookup to the dimensions the constraint is scoped by, or <see langword="null"/> for an unscoped constraint.</param>
    /// <returns>
    /// Tuple containing a boolean saying whether or not its allowed to perform and the <see cref="EventSequenceNumber"/> for the item it violates.
    /// Returns <see cref="EventSequenceNumber.Unavailable"/> if it doesn't exist.
    /// </returns>
    /// <remarks>
    /// Answers exactly what <see cref="IsAllowed"/> answers, with one difference: a scoped definition is answered
    /// within one scope only, so covered events and removal events outside the <paramref name="scope"/> belong to a
    /// different cycle and neither block nor release this one. The scope arrives as the appending event's own typed
    /// dimension values so that an implementation can narrow with a native, indexable predicate on each dimension -
    /// <see cref="ConstraintScope"/> itself only says which dimensions participate and never carries a value to
    /// compare against, and two genuinely different dimension tuples can never compare equal.
    /// <para>
    /// This is the member the kernel calls, and the one a provider should implement. It is deliberately named apart
    /// from <see cref="IsAllowed"/> rather than overloading it, so that no existing call site becomes ambiguous.
    /// </para>
    /// <para>
    /// The default implementation exists only so that a provider written against the original interface still
    /// compiles and still runs: it flattens the scope back into the exact key that provider already received and
    /// forwards to <see cref="IsAllowed"/>. That preserves such a provider's behavior precisely, including the
    /// aliasing weakness of a flattened key - it does not repair it. Scoped isolation is guaranteed only by an
    /// implementation that overrides this member and narrows per typed dimension.
    /// </para>
    /// </remarks>
    Task<(bool IsAllowed, EventSequenceNumber SequenceNumber)> IsAllowedWithinScope(UniqueEventTypeConstraintDefinition definition, EventSourceId eventSourceId, ResolvedConstraintScope? scope = null) =>
        IsAllowed(definition, eventSourceId, scope.ToLegacyScopeKey());
}
