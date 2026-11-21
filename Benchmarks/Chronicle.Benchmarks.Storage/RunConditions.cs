// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>The machine a run happened on and how loaded it was.</summary>
/// <param name="Hardware">The machine.</param>
/// <param name="LoadAtStart">The load average when the run started.</param>
/// <param name="LoadAtEnd">The load average when the run ended.</param>
/// <param name="MaxLoad">The load limit that was applied.</param>
public sealed record RunConditions(HardwareDescription Hardware, LoadReading LoadAtStart, LoadReading? LoadAtEnd, double MaxLoad);
