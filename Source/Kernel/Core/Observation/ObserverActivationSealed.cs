// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when an activation that attempted source deletion receives new work.
/// </summary>
/// <param name="observerKey">The observer whose activation is sealed.</param>
public class ObserverActivationSealed(ObserverKey observerKey) : Exception($"Observer activation '{observerKey}' is sealed; retry on a fresh activation.");
