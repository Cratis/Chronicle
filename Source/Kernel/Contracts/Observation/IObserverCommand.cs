// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Defines the minimum contract for an observer command.
/// </summary>
public interface IObserverCommand
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    string EventStoreName { get; set; }

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the observer identifier.
    /// </summary>
    string ObserverId { get; set; }
}
