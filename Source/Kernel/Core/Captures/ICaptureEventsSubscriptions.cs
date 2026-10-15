// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the subscription of events captures to the inbox they read from.
/// </summary>
public interface ICaptureEventsSubscriptions
{
    /// <summary>
    /// Subscribe an events capture to its inbox in every namespace of the event store. Either every namespace is
    /// subscribed or - when one fails - the ones already subscribed are unsubscribed again and the failure is thrown.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the capture belongs to.</param>
    /// <param name="definition">The <see cref="CaptureDefinition"/> of the capture, which has an events source.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="Engine.UnsupportedCaptureCapability">The definition does not have an events source, or its inbox cannot be resolved.</exception>
    Task Subscribe(EventStoreName eventStore, CaptureDefinition definition);

    /// <summary>
    /// Subscribe an events capture to its inbox in one namespace - used when a namespace is added after the capture started.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the capture belongs to.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to subscribe in.</param>
    /// <param name="definition">The <see cref="CaptureDefinition"/> of the capture, which has an events source.</param>
    /// <returns>Awaitable task.</returns>
    Task Subscribe(EventStoreName eventStore, EventStoreNamespaceName @namespace, CaptureDefinition definition);

    /// <summary>
    /// Recover the subscription of an events capture in every namespace where it is missing, failed or outdated.
    /// Namespaces that are healthy are left alone. Every namespace is attempted before a failure is thrown.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the capture belongs to.</param>
    /// <param name="definition">The <see cref="CaptureDefinition"/> of the capture, which has an events source.</param>
    /// <returns>Awaitable task.</returns>
    Task Recover(EventStoreName eventStore, CaptureDefinition definition);

    /// <summary>
    /// Unsubscribe an events capture from its inbox in every namespace of the event store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the capture belongs to.</param>
    /// <param name="definition">The <see cref="CaptureDefinition"/> of the capture, which has an events source.</param>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe(EventStoreName eventStore, CaptureDefinition definition);

    /// <summary>
    /// Unsubscribe an events capture and remove everything it remembers: the observer, its offset and definition,
    /// and the state it kept per namespace.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the capture belongs to.</param>
    /// <param name="definition">The <see cref="CaptureDefinition"/> of the capture, which has an events source.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(EventStoreName eventStore, CaptureDefinition definition);
}
