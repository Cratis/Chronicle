// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Decisions;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The kernel cannot produce an exact event-log decision read for this read model.
/// </summary>
/// <param name="reason">The typed admission refusal.</param>
public class DecisionReadRefused(DecisionReadRefusal reason) : Exception($"Decision read refused: {reason}.")
{
    /// <summary>
    /// Gets the admission refusal reason.
    /// </summary>
    public DecisionReadRefusal Reason { get; } = reason;
}
