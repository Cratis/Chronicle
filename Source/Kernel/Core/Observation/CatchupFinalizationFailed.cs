// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a catch-up handler cannot finalize a catch-up.
/// </summary>
/// <param name="error">The handler error.</param>
public class CatchupFinalizationFailed(ICanHandleCatchupForObserver.Error error) : Exception($"Could not finalize observer catch-up: {error}");
