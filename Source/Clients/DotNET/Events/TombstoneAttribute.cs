// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Marks an event type as a tombstone, recorded as metadata with the event type registration.
/// </summary>
/// <remarks>
/// The marker is sent only when event types are registered. It does not delete events or read models,
/// change observation, or perform compliance erasure.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class TombstoneAttribute : Attribute;
