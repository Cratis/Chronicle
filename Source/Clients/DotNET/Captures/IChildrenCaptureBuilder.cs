// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the builder for a child capture scope.
/// </summary>
public interface IChildrenCaptureBuilder
{
    /// <summary>
    /// Configures map operations for the child scope.
    /// </summary>
    /// <param name="configure">Callback for configuring the map.</param>
    /// <returns>The builder continuation.</returns>
    IChildrenCaptureBuilder Map(Action<IMapBuilder> configure);

    /// <summary>
    /// Appends an event for the child scope.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to append.</typeparam>
    /// <param name="configure">Callback for configuring the append definition.</param>
    /// <returns>The builder continuation.</returns>
    IChildrenCaptureBuilder Append<TEvent>(Action<IAppendBuilder<TEvent>> configure)
        where TEvent : class;
}
