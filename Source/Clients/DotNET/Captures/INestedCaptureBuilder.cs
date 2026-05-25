// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the builder for a nested capture scope.
/// </summary>
public interface INestedCaptureBuilder
{
    /// <summary>
    /// Configures map operations for the nested scope.
    /// </summary>
    /// <param name="configure">Callback for configuring the map.</param>
    /// <returns>The builder continuation.</returns>
    INestedCaptureBuilder Map(Action<IMapBuilder> configure);

    /// <summary>
    /// Appends an event for the nested scope.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to append.</typeparam>
    /// <param name="configure">Callback for configuring the append definition.</param>
    /// <returns>The builder continuation.</returns>
    INestedCaptureBuilder Append<TEvent>(Action<IAppendBuilder<TEvent>> configure)
        where TEvent : class;
}
