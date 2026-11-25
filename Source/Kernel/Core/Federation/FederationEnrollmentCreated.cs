// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// An enrollment was created for a subject. The event source id is the enrollment id. Only a hash of the token secret is recorded.
/// </summary>
/// <param name="Subject">The opaque subject the enrollment is for.</param>
/// <param name="Instance">The kernel instance the identity is issued for.</param>
/// <param name="TokenHash">The SHA-256 of the token secret, base64.</param>
/// <param name="CreatedAt">When the enrollment was created.</param>
/// <param name="ExpiresAt">When the enrollment can no longer be redeemed.</param>
/// <param name="CreatedBy">The identity that created the enrollment.</param>
[EventType]
public record FederationEnrollmentCreated(string Subject, string Instance, string TokenHash, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, string CreatedBy);
