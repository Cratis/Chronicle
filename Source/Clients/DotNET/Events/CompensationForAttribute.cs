// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute used to mark an event type as a compensation for another event type. Useful in systems
/// that require explicit compensation records, such as ledger systems.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public abstract class CompensationForAttribute : Attribute
{
    /// <summary>
    /// Gets the <see cref="Type"/> of the event that this event compensates for.
    /// </summary>
    public abstract Type CompensatedEventType { get; }
}
