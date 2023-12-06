// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents observer details to be used when performing observer service operations.
/// </summary>
/// <param name="Identifier">The unique <see cref="ObserverId"/> for the observer.</param>
/// <param name="Key">The <see cref="ObserverKey"/> for a specific instance.</param>
/// <param name="Type">The <see cref="ObserverType"/> for the observer.</param>
public record ObserverDetails(ObserverId Identifier, ObserverKey Key, ObserverType Type);
