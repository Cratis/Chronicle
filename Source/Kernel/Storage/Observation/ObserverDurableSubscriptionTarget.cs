// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// A client connection a subscription delivers to, as the facts a restored subscription has to validate.
/// </summary>
/// <param name="ConnectionId">The identifier of the client connection.</param>
/// <param name="Silo">The parsable address of the silo that terminates the connection.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Observation.ObserverDurableSubscriptionTarget")]
public record ObserverDurableSubscriptionTarget([Id(0)] string ConnectionId, [Id(1)] string Silo);
