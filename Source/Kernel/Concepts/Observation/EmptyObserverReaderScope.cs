// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// The exception that is thrown when an explicit empty observer scope attempts to open a reader.
/// </summary>
public class EmptyObserverReaderScope() : Exception("An explicit empty observer scope cannot open a reader; an empty storage filter would read every event type.");