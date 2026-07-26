// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// The exception that is thrown by a substituted appended-events queues grain to simulate a transport failure while
/// handing an already durably appended batch over for live delivery.
/// </summary>
public class SimulatedEnqueueError() : Exception("Simulated transport error while enqueueing appended events");
