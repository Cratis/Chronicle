// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when management cannot confirm durable alert reconciliation.
/// </summary>
/// <param name="observer">The observer whose cleanup must be retried.</param>
public class ObserverAlertsNotReconciled(ObserverKey observer) : Exception($"Alerts for observer '{observer}' are not reconciled. No destructive cleanup is authorized; retry the operation.");
