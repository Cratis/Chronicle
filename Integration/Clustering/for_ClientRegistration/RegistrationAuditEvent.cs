// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientRegistration;

/// <summary>
/// An event the client registers together with <see cref="UniqueRegistrationAuditReference"/>, so the specs can see a
/// constraint travel through client registration.
/// </summary>
/// <param name="Reference">The reference that has to be unique.</param>
[EventType]
public record RegistrationAuditEvent(string Reference);
