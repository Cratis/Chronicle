// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_PIIManager;

/// <summary>
/// A public PII-carrying event for verifying encryption key forwarding through the outbox.
/// </summary>
/// <param name="PersonId">The subject the PII is encrypted under.</param>
/// <param name="Name">A non-PII value that must survive right-to-erasure.</param>
/// <param name="SocialSecurityNumber">The PII value that must become unreadable after erasure.</param>
[EventType]
[Public]
public record PersonRegisteredInOutbox(
    [Subject] string PersonId,
    string Name,
    [property: PII] string SocialSecurityNumber);
