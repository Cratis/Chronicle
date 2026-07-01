// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child line keyed by a <see cref="DateOnly"/> day, materialized from an event on a separate event source.
/// </summary>
/// <param name="Day">The business day, used as the child key.</param>
/// <param name="Amount">The recorded amount.</param>
public record DayLine(
    [Key] DateOnly Day,
    decimal Amount);
