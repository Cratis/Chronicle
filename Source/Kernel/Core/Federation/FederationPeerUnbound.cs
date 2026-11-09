// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// A consumer-side binding was removed. The event source id is the binding id.
/// </summary>
[EventType]
public record FederationPeerUnbound;
