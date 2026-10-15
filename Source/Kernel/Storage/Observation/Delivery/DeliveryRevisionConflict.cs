// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents a root CAS conflict; no row operations were committed.
/// </summary>
/// <param name="Expected">The expected revision.</param>
/// <param name="Actual">The current revision, or None if the root does not exist.</param>
public sealed record DeliveryRevisionConflict(DeliveryRevision Expected, DeliveryRevision Actual);
