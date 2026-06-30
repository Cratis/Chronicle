// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event for starting a timesheet.
/// </summary>
/// <param name="Year">The year the sheet covers.</param>
[EventType]
public record SheetStarted(int Year);
