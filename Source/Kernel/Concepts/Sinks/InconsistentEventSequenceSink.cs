// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Sinks;

/// <summary>
/// The exception that is thrown when a sink definition mixes event-sequence metadata with a read-model sink type, or names the event-sequence sink without a target.
/// </summary>
public class InconsistentEventSequenceSink() : Exception("The sink definition is inconsistent: event-sequence metadata requires the EventSequence sink type and the EventSequence sink type requires an event target. It is refused rather than falling back to a read model or the event log.");
