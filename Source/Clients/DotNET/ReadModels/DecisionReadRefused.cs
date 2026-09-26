// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>Thrown before enrollment when a decision read cannot be protected.</summary>
/// <param name="reason">The refusal reason.</param>
/// <param name="readModelType">The read model type.</param>
public class DecisionReadRefused(DecisionReadRefusalReason reason, Type readModelType)
    : Exception($"Decision read of '{readModelType}' was refused: {reason}.")
{
    /// <summary>Gets the refusal reason.</summary>
    public DecisionReadRefusalReason Reason { get; } = reason;

    /// <summary>Gets the refused model type.</summary>
    public Type ReadModelType { get; } = readModelType;
}
