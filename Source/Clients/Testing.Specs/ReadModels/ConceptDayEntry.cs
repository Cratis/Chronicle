// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child entry for a single worked day on a <see cref="ConceptSheet"/>, carrying a concept-typed value.
/// </summary>
/// <param name="Day">The business day, used as the child key.</param>
/// <param name="Hours">The number of hours worked, as a <see cref="WorkHours"/> concept.</param>
public record ConceptDayEntry(
    DateOnly Day,
    WorkHours Hours);
