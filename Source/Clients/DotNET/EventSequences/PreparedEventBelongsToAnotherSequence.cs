// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when prepared content is used outside its owning event sequence.
/// </summary>
public class PreparedEventBelongsToAnotherSequence() : Exception("Use the event sequence that prepared this event for append and verification.");
