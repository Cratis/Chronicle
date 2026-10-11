// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when pinned delivery has no release boundary.
/// </summary>
public class EventGenerationDeliveryNotSupported() : Exception("Pinned event generation delivery is not supported by this observer.");
