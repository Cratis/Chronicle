// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.EventSequences;

/// <summary>
/// Represents an event to be appended with valid from information.
/// </summary>
/// <param name="Event">The actual event.</param>
/// <param name="ValidFrom">Optionally the associated valid from.</param>
public record EventAndValidFrom(object Event, DateTimeOffset? ValidFrom);
