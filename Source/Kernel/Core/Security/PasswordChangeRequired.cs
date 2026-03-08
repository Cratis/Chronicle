// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the event for when a user is required to change their password.
/// </summary>
[EventType]
public record PasswordChangeRequired();
