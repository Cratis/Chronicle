// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Defines a builder for creating instances of <see cref="ReactorContextValues"/>.
/// </summary>
public interface IReactorContextValuesBuilder
{
    /// <summary>
    /// Creates a new instance of <see cref="ReactorContextValues"/>.
    /// </summary>
    /// <param name="reactor">The reactor instance whose side-effect events are being appended.</param>
    /// <param name="eventContext">The <see cref="EventContext"/> of the triggering event.</param>
    /// <returns>A new instance of <see cref="ReactorContextValues"/>.</returns>
    ReactorContextValues Build(object reactor, EventContext eventContext);
}
