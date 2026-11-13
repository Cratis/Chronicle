// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the outcome of checking event type registrations.
/// </summary>
/// <param name="Results">One result per checked registration, in the order given.</param>
public record CheckEventTypeRegistrationsResult(IEnumerable<EventTypeCheckResult> Results);
