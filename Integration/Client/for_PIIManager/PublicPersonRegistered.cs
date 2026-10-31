// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_PIIManager;

/// <summary>
/// A public registration whose compliance subject and PII are forwarded to another event store.
/// </summary>
/// <param name="PersonId">The subject the PII is encrypted under.</param>
/// <param name="Name">A non-PII value that must survive right-to-erasure.</param>
/// <param name="SocialSecurityNumber">The PII value that must become unreadable after erasure.</param>
[EventType]
[Public]
public record PublicPersonRegistered(
    [Subject] string PersonId,
    string Name,
    [property: PII] string SocialSecurityNumber);
