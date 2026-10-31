// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// The durable definition and effective-from instant. Rule and policy are opaque JSON owned by the schedule engine.
/// </summary>
/// <param name="Token">The stable token.</param>
/// <param name="Revision">The registered revision.</param>
/// <param name="Fingerprint">The normalized definition fingerprint.</param>
/// <param name="Definition">The JSON rule, catch-up policy, scope and migration directive.</param>
/// <param name="EffectiveFrom">The durable start instant.</param>
/// <param name="Retired">Whether materialization is stopped.</param>
public record ScheduleDefinition(
    ScheduleToken Token,
    long Revision,
    string Fingerprint,
    string Definition,
    DateTimeOffset EffectiveFrom,
    bool Retired);
