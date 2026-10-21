// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when buffered observer writes cannot be confirmed durable.
/// </summary>
public class ObserverFlushFailed() : Exception("Buffered observer writes could not be confirmed durable.");
