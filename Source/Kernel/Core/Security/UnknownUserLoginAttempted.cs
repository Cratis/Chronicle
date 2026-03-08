// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the event for when an unknown user login is attempted.
/// </summary>
/// <param name="Username">The attempted username.</param>
[EventType]
public record UnknownUserLoginAttempted(Username Username);
