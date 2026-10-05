// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// The exception that is thrown when an incident sequence cannot be stored as signed 64-bit.
/// </summary>
/// <param name="value">The rejected sequence number.</param>
public class AlertIncidentSequenceNumberOutOfRange(EventSequenceNumber value)
    : Exception($"Incident sequence number {value.Value} exceeds {long.MaxValue}.");
