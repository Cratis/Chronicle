// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// The different append event result error types.
/// </summary>
public enum AppendedEventError
{
    /// <summary>
    /// The <see cref="EventSequenceNumber"/> was duplicated.
    /// </summary>
    DuplicateEventSequenceNumber = 0
}
