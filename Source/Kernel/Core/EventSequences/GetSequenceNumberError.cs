// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The errors that can occur when getting <see cref="EventSequenceNumber"/> from <see cref="IEventSequence"/>.
/// </summary>
public enum GetSequenceNumberError
{
    /// <summary>
    /// The error when it's an unexpected error from storage layer.
    /// </summary>
    StorageError = 0,

    /// <summary>
    /// The error when the event sequence number matching the given filter was not wound.
    /// </summary>
    NotFound = 1
}
