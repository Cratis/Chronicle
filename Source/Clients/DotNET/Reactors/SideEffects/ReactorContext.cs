// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents the combined context available to a <see cref="IReactorSideEffectHandler"/> when processing
/// a value returned by a reactor handler method.
/// </summary>
/// <param name="EventContext">The <see cref="Events.EventContext"/> of the event that triggered the reactor.</param>
/// <param name="Reactor">The reactor instance that handled the event.</param>
/// <param name="Values">The <see cref="ReactorContextValues"/> carrying append-metadata resolved for the side-effect.</param>
public record ReactorContext(EventContext EventContext, object Reactor, ReactorContextValues Values);
