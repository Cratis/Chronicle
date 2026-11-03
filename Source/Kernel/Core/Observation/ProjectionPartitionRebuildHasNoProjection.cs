// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when an isolated partition rebuild begins for a projection that has no current definition.
/// </summary>
/// <param name="observerKey">The <see cref="ObserverKey"/> of the projection.</param>
public class ProjectionPartitionRebuildHasNoProjection(ObserverKey observerKey)
    : Exception($"Cannot rebuild a partition of projection '{observerKey.ObserverId}' in event store '{observerKey.EventStore}' and namespace '{observerKey.Namespace}' in isolation, it has no current definition");
