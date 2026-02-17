// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Represents the event for the initial admin user that has been added without a password.
/// </summary>
/// <param name="Username">The username.</param>
/// <param name="Email">The user's email address.</param>
[EventType]
public record InitialAdminUserAdded(Username Username, UserEmail Email);
