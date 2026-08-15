// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event that revokes the badge held by a <see cref="SecurityPass"/>.
/// </summary>
[EventType]
public record SecurityBadgeRevoked;
