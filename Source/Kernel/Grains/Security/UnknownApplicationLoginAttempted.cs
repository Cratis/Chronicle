// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Represents the event for when an unknown application login is attempted.
/// </summary>
/// <param name="ClientId">The attempted client identifier.</param>
[EventType]
public record UnknownApplicationLoginAttempted(ClientId ClientId);
