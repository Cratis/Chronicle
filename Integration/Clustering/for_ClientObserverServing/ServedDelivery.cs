// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing;

/// <summary>
/// One delivery to a client instance.
/// </summary>
/// <param name="Instance">The client instance that received it.</param>
/// <param name="Source">The event source id.</param>
/// <param name="Sequence">The sequence carried by the event.</param>
public record ServedDelivery(string Instance, string Source, int Sequence);
