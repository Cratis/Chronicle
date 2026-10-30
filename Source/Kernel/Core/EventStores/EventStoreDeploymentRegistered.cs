// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Registrations;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the deployment state registered for an event store in the System event sequence.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Generation">The highest accepted deployment generation.</param>
/// <param name="Fingerprint">The latest registered fingerprint.</param>
[EventType]
public record EventStoreDeploymentRegistered(EventStoreName EventStore, DeploymentGeneration Generation, ArtifactFingerprint Fingerprint);
