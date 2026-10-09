// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Exception that gets thrown when an event an events capture observes does not resolve a key.
/// </summary>
/// <param name="keyProperty">The key expression of the capture.</param>
/// <param name="eventType">The event type that did not resolve a key.</param>
/// <param name="sequenceNumber">The sequence number of the event in the source sequence.</param>
public class MissingKeyForCapturedEvent(string keyProperty, string eventType, ulong sequenceNumber)
    : Exception($"The event '{eventType}' at sequence number {sequenceNumber} has no value for the capture key '{keyProperty}'");
