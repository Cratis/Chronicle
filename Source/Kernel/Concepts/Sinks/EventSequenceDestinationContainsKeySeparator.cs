// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Sinks;

/// <summary>
/// The exception that is thrown when an event target names a destination containing the key separator.
/// </summary>
/// <param name="destination">The refused <see cref="EventSequenceId"/>.</param>
public class EventSequenceDestinationContainsKeySeparator(EventSequenceId destination) : Exception($"The event sequence sink destination '{destination}' contains '{KeyHelper.Separator}', which is reserved as a key separator and would address another sequence. Choose a different name.");
