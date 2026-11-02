// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Chronicle.Benchmarks.NormalClient;

/// <summary>What one measured phase cost.</summary>
/// <param name="ThreadBytes">Bytes allocated on the measuring thread.</param>
/// <param name="ProcessBytes">Bytes allocated by the whole process.</param>
/// <param name="Milliseconds">Elapsed milliseconds.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct Cost(long ThreadBytes, long ProcessBytes, double Milliseconds);
