// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts;

record RowVerdict(string Id, RowVerdictKind Kind, int Tests, int Planted, string Reason)
{
    public bool Fails(string mode) => Kind is RowVerdictKind.Unowned or RowVerdictKind.Red or RowVerdictKind.StalePending
        || (string.Equals(mode, "release", StringComparison.Ordinal) && (Kind is RowVerdictKind.Pending or RowVerdictKind.Skipped));
}
