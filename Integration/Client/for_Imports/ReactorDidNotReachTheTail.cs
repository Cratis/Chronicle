// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Imports;

/// <summary>
/// The exception that is thrown when a reactor has not handled every event within the wait, saying where it stood so a slow
/// reactor can be told from a stalled one.
/// </summary>
/// <param name="tail">The sequence number the reactor was expected to reach.</param>
/// <param name="state">The state of the reactor when the wait ran out.</param>
/// <param name="handled">How many events the reactor had handled.</param>
/// <param name="committedAfter">How long the specification had run when the import was committed.</param>
/// <param name="elapsed">How long the specification had run.</param>
public class ReactorDidNotReachTheTail(EventSequenceNumber tail, ReactorState state, int handled, TimeSpan committedAfter, TimeSpan elapsed)
    : Exception($"The reactor did not reach {tail} after {elapsed}, the import having committed after {committedAfter}: it is {state.RunningState}, subscribed {state.IsSubscribed}, last handled {state.LastHandledEventSequenceNumber}, next {state.NextEventSequenceNumber}, tail {state.TailEventSequenceNumber}, and had handled {handled} events");
