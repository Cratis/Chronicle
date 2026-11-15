// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// One direction of the policy of a subject was revoked. The other direction is not affected. The event source id is the subject.
/// </summary>
/// <param name="Direction">The direction that was revoked.</param>
/// <param name="RevokedBy">The identity that revoked the direction.</param>
[EventType]
public record FederationPolicyDirectionRevoked(FederationDirection Direction, string RevokedBy);
