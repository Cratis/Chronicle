// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// A producer-side link was revoked. The event source id is the link id. Setting the grant again reinstates it.
/// </summary>
[EventType]
public record FederationLinkRevoked;
