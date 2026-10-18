// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Signals that the provider proved no events from the attempted append were committed.
/// </summary>
/// <param name="innerException">The failure that prevented the write.</param>
public class EventsNotCommitted(Exception innerException) : Exception(innerException.Message, innerException);
