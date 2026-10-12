// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_a_member_with_a_derived_function_name.given;

/// <summary>
/// Records the scheduled week number.
/// </summary>
/// <param name="Week">The week that was scheduled.</param>
[EventType]
public record WeekScheduled(int Week);
