// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Represents evidence recorded with an incident.
/// </summary>
/// <param name="AttemptCount">Recorded attempts.</param>
/// <param name="FirstFailure">First failure time.</param>
/// <param name="LastFailure">Last failure time.</param>
/// <param name="FailureKind">Recorded failure kind.</param>
/// <param name="Message">Recorded failure message.</param>
public record AlertIncidentEvidence(
    int AttemptCount,
    DateTimeOffset FirstFailure,
    DateTimeOffset LastFailure,
    FailureKind FailureKind,
    string Message);
