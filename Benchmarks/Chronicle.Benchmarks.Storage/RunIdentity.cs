// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Which run a ledger describes.</summary>
/// <param name="Profile">The profile name.</param>
/// <param name="ChronicleCommit">The Chronicle commit the harness was built from.</param>
/// <param name="StartedUtc">When the run started.</param>
public sealed record RunIdentity(string Profile, string ChronicleCommit, DateTimeOffset StartedUtc);
