// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Local diagnostic only. One consistent capture of a <see cref="StartupDiagnosticRecorder"/>.
/// </summary>
/// <param name="Records">The reserved records; unpublished slots have outcome zero.</param>
/// <param name="Dropped">The number of spans lost to overflow.</param>
/// <param name="Active">The number of reserved spans not yet published.</param>
/// <param name="Capacity">The recorder capacity.</param>
internal sealed record StartupDiagnosticSnapshot(StartupDiagnosticRecord[] Records, long Dropped, int Active, int Capacity)
{
    /// <summary>
    /// Gets whether attribution from this capture is complete and usable.
    /// </summary>
    internal bool Valid => Dropped == 0 && Active == 0 && Records.All(record => record.Outcome != 0 && record.End >= record.Start);
}
