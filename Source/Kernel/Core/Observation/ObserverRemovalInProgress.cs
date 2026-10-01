// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a subscription would cross an unfinished removal fence.
/// </summary>
/// <param name="observer">The observer being removed.</param>
public class ObserverRemovalInProgress(ObserverKey observer) : Exception($"Observer '{observer}' is being removed; retry after cleanup completes.");
