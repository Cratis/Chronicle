// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Decisions;

/// <summary>
/// Constructs typed refusals without exposing helpers as generated query methods.
/// </summary>
internal static class DecisionReadRefusals
{
    /// <summary>
    /// Creates a response with no usable instance or concurrency scope.
    /// </summary>
    /// <param name="refusal">The refusal reason.</param>
    /// <returns>A refused decision read.</returns>
    internal static DecisionReadModel Refuse(DecisionReadRefusal refusal) => new("null", EventSequenceNumber.Unavailable, [], refusal);
}
