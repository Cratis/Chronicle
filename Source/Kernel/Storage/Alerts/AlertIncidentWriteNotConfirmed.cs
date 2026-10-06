// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// The exception that is thrown when a conditional write cannot be confirmed.
/// </summary>
/// <param name="id">The incident whose write outcome is unknown.</param>
public class AlertIncidentWriteNotConfirmed(IncidentId id)
    : Exception($"The write for incident {id} could not be confirmed.");
