// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the event for a user that has been authenticated.
/// </summary>
/// <param name="Username">The username.</param>
[EventType]
public record UserAuthenticated(Username Username);
