// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// One cursor call, including the final empty probe when present.
/// </summary>
/// <param name="Index">The zero-based page index.</param>
/// <param name="Start">The requested starting sequence number.</param>
/// <param name="Events">The number of events returned, or zero for the terminal probe.</param>
/// <param name="Milliseconds">Elapsed MoveNext time, including storage reads and conversion.</param>
internal sealed record DrainPage(int Index, ulong Start, int Events, double Milliseconds);
