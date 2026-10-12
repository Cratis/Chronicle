// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_a_function_named_member.given;

/// <summary>
/// Records a week number for a calendar.
/// </summary>
/// <param name="Week">The recorded week number.</param>
[EventType]
public record WeekRecorded(int Week);
