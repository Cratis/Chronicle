// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_event_with_registered_migration;

/// <summary>
/// Generation 1 of <see cref="ContractSigned"/>. Has only a contract identifier.
/// Generation 2 adds a <see cref="Status"/> property, defaulting to <c>pending</c> for events
/// stored before the property existed.
/// </summary>
/// <param name="ContractId">The contract identifier.</param>
[EventType]
public record ContractSigned(string ContractId);
