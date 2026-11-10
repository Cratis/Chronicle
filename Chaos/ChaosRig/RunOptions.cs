// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Defines one reproducible chaos run.
/// </summary>
/// <param name="Seed">Fault schedule seed.</param>
/// <param name="Duration">Chaos window.</param>
/// <param name="Settle">Maximum drain time.</param>
/// <param name="Faults">Enabled faults.</param>
/// <param name="Output">Output directory.</param>
/// <param name="KeepContainers">Whether to retain Docker resources.</param>
/// <param name="SelfTestViolation">Whether to inject a snapshot violation.</param>
internal sealed record RunOptions(int Seed, TimeSpan Duration, TimeSpan Settle, IReadOnlyList<FaultKind> Faults, string Output, bool KeepContainers, bool SelfTestViolation);
