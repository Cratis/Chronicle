// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents observer details to be used when performing observer service operations.
/// </summary>
/// <param name="Key">The <see cref="ObserverKey"/> for a specific instance.</param>
/// <param name="Type">The <see cref="ObserverType"/> for the observer.</param>
public record ObserverDetails(ObserverKey Key, ObserverType Type);
