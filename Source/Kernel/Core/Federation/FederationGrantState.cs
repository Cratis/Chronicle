// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The effective producer grant after folding all events and configuration.
/// </summary>
public record FederationGrantState(
    string Id,
    string ClientPin,
    string EventStore,
    IReadOnlyList<string> Namespaces,
    bool AllPublic,
    IReadOnlyList<string> EventTypes,
    bool Revoked,
    string Origin);
