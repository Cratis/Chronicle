// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the full identifier with key for an observer.
/// </summary>
/// <param name="ObserverId"><see cref="ObserverId"/> that identifies the observer.</param>
/// <param name="ObserverKey"><see cref="ObserverKey"/> that identifies the instance of the observer.</param>
public record ObserverIdAndKey(ObserverId ObserverId, ObserverKey ObserverKey);
