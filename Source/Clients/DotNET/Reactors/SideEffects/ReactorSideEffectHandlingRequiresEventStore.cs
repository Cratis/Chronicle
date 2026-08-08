// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// The exception that is thrown when a reactor side effect is offered to a handler without the event store it
/// belongs to, and the handler does not implement the event-store-less contract it was offered through.
/// </summary>
/// <param name="type">The <see cref="Type"/> that was asked.</param>
public class ReactorSideEffectHandlingRequiresEventStore(Type type)
    : Exception($"'{type.FullName}' was asked whether it can handle a reactor side effect without being given the event store. Whether a value is a known event type is answered by the event type registry, which belongs to the event store - and therefore the namespace - the current scope resolved, so there is no answer without one. Call the CanHandle overload that takes the IEventStore.");
