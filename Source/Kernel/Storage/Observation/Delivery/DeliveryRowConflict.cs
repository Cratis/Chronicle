// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// The exception that is thrown when a strict delivery row insert collides; the entire commit is rolled back.
/// </summary>
/// <param name="identity">The conflicting row identity.</param>
public sealed class DeliveryRowConflict(string identity) : Exception($"Observer delivery row '{identity}' already exists; the commit was rolled back.");
