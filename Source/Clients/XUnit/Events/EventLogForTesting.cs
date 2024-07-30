// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventLog"/> for testing.
/// </summary>
public class EventLogForTesting : EventSequenceForTesting, IEventLog;
