// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an event that was appended to an event sequence together with the result of the operation.
/// </summary>
/// <param name="Event">The <see cref="AppendedEvent"/> that was appended.</param>
/// <param name="Result">The <see cref="AppendResult"/> describing success, violations, or errors.</param>
public record AppendedEventWithResult(AppendedEvent Event, AppendResult Result);
