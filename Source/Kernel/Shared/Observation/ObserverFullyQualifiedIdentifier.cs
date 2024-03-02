// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents a fully qualified identifier of an observer.
/// </summary>
/// <param name="ObserverId">The unique <see cref="ObserverId"/>.</param>
/// <param name="ObserverKey">The <see cref="ObserverKey"/> part.</param>
public record ObserverFullyQualifiedIdentifier(ObserverId ObserverId, ObserverKey ObserverKey);
