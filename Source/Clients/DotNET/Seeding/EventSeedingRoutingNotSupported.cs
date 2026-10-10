// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Thrown when a seeding builder does not support routed events.
/// </summary>
public class EventSeedingRoutingNotSupported() : Exception("This event seeding implementation does not support event routing.");
