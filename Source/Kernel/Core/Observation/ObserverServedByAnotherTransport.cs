// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Exception that occurs when a webhook and a connected client attempt to serve the same observer.
/// </summary>
/// <param name="observerKey">The observer already served by another transport.</param>
[GenerateSerializer]
public class ObserverServedByAnotherTransport(ObserverKey observerKey)
    : Exception($"Observer '{observerKey}' is already served by another transport. Remove the webhook or disconnect the client before changing transports.");
