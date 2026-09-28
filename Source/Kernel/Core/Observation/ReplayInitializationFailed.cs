// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a replay handler cannot begin a replay.
/// </summary>
/// <param name="error">The handler error.</param>
public class ReplayInitializationFailed(ICanHandleReplayForObserver.Error error) : Exception($"Could not begin observer replay: {error}");
