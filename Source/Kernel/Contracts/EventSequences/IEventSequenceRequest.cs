// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Defines the common contract for requests working with event sequences.
/// </summary>
public interface IEventSequenceRequest
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
    /// Gets or sets the event sequence identifier.
    /// </summary>
    string EventSequenceId { get; set; }
}
