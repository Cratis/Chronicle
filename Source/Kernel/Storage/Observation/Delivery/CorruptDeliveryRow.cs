// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// The exception that is thrown when stored delivery values are invalid and a read fails closed.
/// </summary>
/// <param name="field">The invalid stored field.</param>
public sealed class CorruptDeliveryRow(string field) : Exception($"Stored observer delivery field '{field}' is corrupt.");
