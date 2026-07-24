// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// The exception that is thrown by a substituted storage to simulate a non-duplicate (transient/aborted) failure
/// while appending a batch of events.
/// </summary>
public class SimulatedStorageError() : Exception("Simulated non-duplicate storage error");
