// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The effective consumer binding after folding all events and configuration.
/// </summary>
/// <param name="Id">The binding id.</param>
/// <param name="LogicalSource">The logical source name consumer code refers to.</param>
/// <param name="Endpoint">The federation endpoint of the peer.</param>
/// <param name="ServerPin">The pinned server certificate, <c language="text">sha256:&lt;base64&gt;</c>.</param>
/// <param name="ClientCertificate">The name of the configured client certificate to present.</param>
/// <param name="RemoteEventStore">The event store on the peer.</param>
/// <param name="RemoteNamespace">The namespace on the peer.</param>
/// <param name="TargetEventStore">The local event store whose inbox receives the events.</param>
/// <param name="TargetNamespace">The local namespace.</param>
/// <param name="Origin">Where this came from, see <see cref="FederationOrigin"/>.</param>
public record FederationBindingState(
    string Id,
    string LogicalSource,
    string Endpoint,
    string ServerPin,
    string ClientCertificate,
    string RemoteEventStore,
    string RemoteNamespace,
    string TargetEventStore,
    string TargetNamespace,
    string Origin);
