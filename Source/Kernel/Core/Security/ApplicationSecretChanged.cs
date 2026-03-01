// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the event for an application secret that has been changed.
/// </summary>
/// <param name="ClientSecret">The new hashed client secret.</param>
[EventType]
public record ApplicationSecretChanged(ClientSecret ClientSecret);
