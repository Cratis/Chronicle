// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute used to mark a type as representing a previous generation of <typeparamref name="TEventType"/>.
/// </summary>
/// <remarks>
/// Place this on a previous-generation record instead of <see cref="EventTypeAttribute"/>. The event type
/// id is resolved from <typeparamref name="TEventType"/>'s own <see cref="EventTypeAttribute"/>, so the two
/// generations can never drift apart by a hand-typed, mismatched id string.
/// </remarks>
/// <typeparam name="TEventType">The event type this is a generation for.</typeparam>
/// <param name="generation">The generation this type represents.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class EventTypeGenerationForAttribute<TEventType>(uint generation) : EventTypeGenerationForAttribute(generation)
    where TEventType : class
{
    /// <inheritdoc/>
    public override Type EventTypeClrType => typeof(TEventType);
}
