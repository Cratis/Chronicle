// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The policy of a subject was revoked for good. The event source id is the subject.
/// </summary>
/// <param name="RevokedBy">The identity that revoked the policy.</param>
[EventType]
public record FederationPolicyRevoked(string RevokedBy);
