// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event for recording hours worked on a specific day.
/// </summary>
/// <param name="Day">The business day the hours were worked, used as the child key.</param>
/// <param name="Hours">The number of hours worked.</param>
[EventType]
public record DayWorked(DateOnly Day, decimal Hours);
