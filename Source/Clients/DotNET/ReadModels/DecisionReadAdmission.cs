// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>The result of checking a model's projection shape without I/O.</summary>
/// <param name="IsAdmitted">Whether the model is admitted.</param>
/// <param name="Reason">The reason if not admitted.</param>
public record DecisionReadAdmission(bool IsAdmitted, DecisionReadRefusalReason? Reason);
