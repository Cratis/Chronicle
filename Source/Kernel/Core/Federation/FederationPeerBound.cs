// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// A consumer-side binding of a logical source name to a physical peer was created or changed.
/// </summary>
/// <param name="LogicalSource">The logical source name consumer code refers to, for example in <c language="csharp">[EventStore("service-name")]</c>.</param>
/// <param name="Endpoint">The federation endpoint of the peer, for example <c language="text">https://peer.example.com:35100</c>.</param>
/// <param name="ServerPin">The pinned server certificate, <c language="text">sha256:&lt;base64&gt;</c>.</param>
/// <param name="ClientCertificate">The name of the configured client certificate to present. A name, never a secret value.</param>
/// <param name="RemoteEventStore">The event store on the peer whose outbox is consumed.</param>
/// <param name="RemoteNamespace">The namespace on the peer.</param>
/// <param name="TargetEventStore">The local event store whose inbox receives the events.</param>
/// <param name="TargetNamespace">The local namespace.</param>
/// <param name="Origin">Where this came from, see <see cref="FederationOrigin"/>.</param>
[EventType]
public record FederationPeerBound(
    string LogicalSource,
    string Endpoint,
    string ServerPin,
    string ClientCertificate,
    string RemoteEventStore,
    string RemoteNamespace,
    string TargetEventStore,
    string TargetNamespace,
    string Origin);
