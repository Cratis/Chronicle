// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// An enrollment was revoked. The event source id is the enrollment id. The certificates issued from it are revoked by their own events.
/// </summary>
/// <param name="RevokedBy">The identity that revoked the enrollment.</param>
[EventType]
public record FederationEnrollmentRevoked(string RevokedBy);
