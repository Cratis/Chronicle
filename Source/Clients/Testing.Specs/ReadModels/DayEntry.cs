// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child entry for a single worked day on a <see cref="Sheet"/>.
/// </summary>
/// <param name="Day">The business day, used as the child key.</param>
/// <param name="Hours">The number of hours worked.</param>
public record DayEntry(
    DateOnly Day,
    decimal Hours);
