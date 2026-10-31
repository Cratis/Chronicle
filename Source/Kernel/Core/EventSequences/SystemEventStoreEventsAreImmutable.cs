// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception thrown when a System event is redacted or revised.
/// </summary>
public class SystemEventStoreEventsAreImmutable() : Exception("System event store events cannot be redacted or revised.");
