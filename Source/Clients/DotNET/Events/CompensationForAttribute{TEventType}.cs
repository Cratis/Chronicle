// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute used to mark an event type as a compensation for <typeparamref name="TEventType"/>.
/// Useful in systems that require explicit compensation records, such as ledger systems.
/// </summary>
/// <remarks>
/// The compensated event type information is passed along as metadata in the event type JSON schema.
/// </remarks>
/// <typeparam name="TEventType">The event type that this event compensates for.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CompensationForAttribute<TEventType> : CompensationForAttribute
    where TEventType : class
{
    /// <inheritdoc/>
    public override Type CompensatedEventType => typeof(TEventType);
}
