// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Represents the payload of an event type registration.
/// </summary>
/// <param name="Type">The type of the event.</param>
/// <param name="Schema">The JSON schema of the event type.</param>
public record EventTypeRegistration(EventType Type, string Schema);
