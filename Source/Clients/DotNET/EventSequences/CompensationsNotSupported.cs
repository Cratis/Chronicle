// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when an event sequence implementation cannot append compensating events.
/// </summary>
/// <param name="implementation">The implementation that cannot compensate.</param>
public class CompensationsNotSupported(Type implementation)
    : Exception($"Event sequence implementation '{implementation.FullName}' does not support compensations.");
