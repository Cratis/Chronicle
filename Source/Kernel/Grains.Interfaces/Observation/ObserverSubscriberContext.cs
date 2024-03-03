// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents the context for an observer subscriber.
/// </summary>
/// <param name="Metadata">Optional metadata associated.</param>
public record ObserverSubscriberContext(object? Metadata);
