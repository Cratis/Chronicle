// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Provides values for a <see cref="ReactorContext"/>.
/// </summary>
public interface IReactorContextValuesProvider
{
    /// <summary>
    /// Gets the reactor context values.
    /// </summary>
    /// <param name="reactor">The reactor instance whose side-effect events are being appended.</param>
    /// <param name="eventContext">The <see cref="EventContext"/> of the triggering event.</param>
    /// <returns>The reactor context values.</returns>
    ReactorContextValues Provide(object reactor, EventContext eventContext);
}
