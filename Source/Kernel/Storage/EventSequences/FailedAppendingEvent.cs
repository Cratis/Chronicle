// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Exception that gets thrown when an unknown.
/// </summary>
public class FailedAppendingEvent() : Exception("Failed appending event to event sequence");
