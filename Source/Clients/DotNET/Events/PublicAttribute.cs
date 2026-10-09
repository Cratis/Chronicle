// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute to adorn an event type to declare it public: part of the contract the owning service exposes.
/// </summary>
/// <remarks>
/// An event type is also public when its <see cref="EventStoreAttribute"/> names the event store the client is
/// connected to. Only public event types may be published to the outbox.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PublicAttribute : Attribute;
