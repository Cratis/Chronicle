// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Delegate that gets called when a replay state notification is to be sent to the client.
/// </summary>
/// <param name="replayState">The <see cref="ReplayState"/> of the notification.</param>
/// <param name="partition">The partition key for partition-specific notifications, or empty string for observer-level notifications.</param>
public delegate void ReactorReplayObserver(ReplayState replayState, string partition);
