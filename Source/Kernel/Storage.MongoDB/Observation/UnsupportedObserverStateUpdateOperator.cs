// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// The exception that is thrown when guarding observer control would discard an update operator.
/// </summary>
/// <param name="updateOperator">The unsupported operator.</param>
public class UnsupportedObserverStateUpdateOperator(string updateOperator)
    : Exception($"Observer type-set control updates support only $set; found '{updateOperator}'.");
