// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// The exception that is thrown when an event store cannot create an independent event-seeding buffer.
/// </summary>
/// <param name="eventStoreType">The event-store implementation that cannot create the buffer.</param>
public class CannotCreateEventSeeding(Type eventStoreType)
    : Exception($"Event store implementation '{eventStoreType.FullName}' does not use Chronicle's event-seeding implementation and cannot create an independent seeding buffer.");
