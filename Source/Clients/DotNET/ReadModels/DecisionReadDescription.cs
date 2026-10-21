// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Describes local decision-read admission without contacting the kernel.
/// </summary>
/// <param name="Admission">The projection shape admission.</param>
/// <param name="EventTypes">The event types watched by an admitted read.</param>
/// <param name="UsesPinnedGenerationDelivery">Whether the definition requires pinned delivery.</param>
public record DecisionReadDescription(DecisionReadAdmission Admission, IReadOnlyList<EventType> EventTypes, bool UsesPinnedGenerationDelivery);
