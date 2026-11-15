// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Thrown when an event sequence implementation does not support copying events between event sources.
/// </summary>
public class EventCopyNotSupported() : Exception("This event sequence implementation does not support copying events.");
