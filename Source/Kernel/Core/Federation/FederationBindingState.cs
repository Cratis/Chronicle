// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The effective consumer binding after folding all events and configuration.
/// </summary>
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
