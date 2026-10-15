// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Exception that gets thrown when the private events translated from an incoming event could not be appended.
/// </summary>
/// <param name="capture">The <see cref="CaptureName"/> of the capture.</param>
/// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the incoming event.</param>
/// <param name="errors">The reasons the append failed.</param>
public class CapturedEventsNotAppended(CaptureName capture, EventSequenceNumber sequenceNumber, string errors)
    : Exception($"The capture '{capture}' could not append the events for the incoming event at sequence number {sequenceNumber}: {errors}");
