// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a replay handler cannot finalize a replay.
/// </summary>
/// <param name="error">The handler error.</param>
public class ReplayFinalizationFailed(ICanHandleReplayForObserver.Error error) : Exception($"Could not finalize observer replay: {error}");
