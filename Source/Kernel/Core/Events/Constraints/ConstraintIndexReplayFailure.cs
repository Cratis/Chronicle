// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Identifies a constraint's failure independently of other failures and abandoned writes in a replay turn.
/// </summary>
/// <param name="Position">The failing event or deciding key position, or null when a cursor fault identifies no event.</param>
/// <param name="Error">The storage or content error.</param>
public record ConstraintIndexReplayFailure(EventSequenceNumber? Position, Exception Error);
