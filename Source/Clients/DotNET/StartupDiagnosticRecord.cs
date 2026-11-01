// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Chronicle;

/// <summary>
/// Local diagnostic only. One numeric, finished (or, with outcome 0, unpublished) operation span.
/// </summary>
/// <param name="Id">The one-based reservation identifier.</param>
/// <param name="ParentId">The enclosing operation's identifier, or zero.</param>
/// <param name="Operation">The operation.</param>
/// <param name="Start">The monotonic start tick.</param>
/// <param name="End">The monotonic end tick.</param>
/// <param name="Outcome">Zero when unpublished, 1 success, 2 failure, 3 canceled.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct StartupDiagnosticRecord(long Id, long ParentId, StartupOperation Operation, long Start, long End, int Outcome);
