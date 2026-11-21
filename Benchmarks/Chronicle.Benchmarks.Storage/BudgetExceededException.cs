// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Signals a profile budget cancellation rather than a measured operation failure.</summary>
/// <param name="budget">The exhausted budget.</param>
public sealed class BudgetExceededException(TimeSpan budget) : OperationCanceledException($"profile budget of {budget.TotalMinutes:0.##} minutes exhausted");
