// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents the actual dimension values a constraint is scoped by for one specific event.
/// </summary>
/// <param name="EventSourceType">The <see cref="Events.EventSourceType"/> the constraint is narrowed to, or <see langword="null"/> when it does not participate.</param>
/// <param name="EventStreamType">The <see cref="Events.EventStreamType"/> the constraint is narrowed to, or <see langword="null"/> when it does not participate.</param>
/// <param name="EventStreamId">The <see cref="Events.EventStreamId"/> the constraint is narrowed to, or <see langword="null"/> when it does not participate.</param>
/// <remarks>
/// A constraint that is not scoped at all has no resolved scope: <see langword="null"/> means "narrow nothing",
/// mirroring <see cref="UniqueConstraintDefinition.Scope"/> and its siblings, which are <see langword="null"/> for
/// an unscoped definition.
/// <para>
/// A <see cref="ConstraintScope"/> on a definition records only <em>which</em> dimensions participate in scoping -
/// the client writes a presence marker into each participating dimension, never a real value. The values themselves
/// belong to the event being appended, so this is what a storage provider needs to narrow a lookup: a dimension
/// that participates carries the appending event's own value, and one that does not is <see langword="null"/>,
/// meaning "do not narrow by it". Being a separate type from <see cref="ConstraintScope"/> is deliberate - it makes
/// it impossible to compare a declaration's presence markers against real event values by accident.
/// </para>
/// <para>
/// Dimensions are kept apart rather than flattened into a single key so that two different tuples can never
/// compare equal: a value that happens to contain whatever character a flat key joins on would otherwise alias
/// onto a genuinely different tuple.
/// </para>
/// </remarks>
/// <example>
/// A constraint declared per event stream id, resolved for an event appended to the <c language="csharp">branch-1</c> stream, is
/// <c language="csharp">new ResolvedConstraintScope(EventStreamId: "branch-1")</c> - the other dimensions stay <see langword="null"/>
/// and never narrow the lookup.
/// </example>
public record ResolvedConstraintScope(
    EventSourceType? EventSourceType = default,
    EventStreamType? EventStreamType = default,
    EventStreamId? EventStreamId = default);
