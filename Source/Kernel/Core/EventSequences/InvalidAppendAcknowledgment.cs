// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when storage acknowledges a different number of events than were submitted.
/// </summary>
/// <param name="expected">The number of submitted events.</param>
/// <param name="actual">The number of acknowledged events.</param>
public class InvalidAppendAcknowledgment(int expected, int actual)
    : Exception($"Storage acknowledged {actual} events for a batch of {expected}. Events may already be persisted; reconcile the outcome before retrying.");
