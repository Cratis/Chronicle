// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when an observer becomes subscribed or active before removal can be fenced.
/// </summary>
/// <param name="observer">The observer which cannot be removed.</param>
public class ObserverRemovalNotAllowed(ObserverKey observer) : Exception($"Observer '{observer}' is subscribed or active and cannot be removed.");
