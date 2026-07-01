// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event assigning a contact, keyed by a <see cref="ContactId"/> concept over a <see cref="Guid"/>.
/// </summary>
/// <param name="ContactId">The contact identifier, used as the child key.</param>
/// <param name="Name">The contact name.</param>
[EventType]
public record ContactAssigned(ContactId ContactId, string Name);
