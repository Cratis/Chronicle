// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Represents one change an appended event makes to a unique constraint index.
/// </summary>
/// <param name="Kind">The <see cref="UniqueConstraintIndexChangeKind"/>.</param>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> the change is for.</param>
/// <param name="SequenceNumber">The <see cref="EventSequenceNumber"/> of the event making the change.</param>
/// <param name="ScopeKey">The resolved scope the change applies to.</param>
/// <param name="Value">The hashed value claimed or released; not used by <see cref="UniqueConstraintIndexChangeKind.Remove"/>.</param>
/// <param name="Subject">The <see cref="Subject"/> whose personal data a claimed value derives from, or null.</param>
public record UniqueConstraintIndexChange(
    UniqueConstraintIndexChangeKind Kind,
    EventSourceId EventSourceId,
    EventSequenceNumber SequenceNumber,
    string ScopeKey,
    UniqueConstraintValue? Value = default,
    Subject? Subject = default);
