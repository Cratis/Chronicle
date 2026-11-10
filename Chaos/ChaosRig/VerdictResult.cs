// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

internal sealed record VerdictResult(int Lost, int Duplicated, int Gaps, int ObserversBehind, int ProjectionsBehind, int ReducersBehind, int FailedPartitions, int ReactorDuplicates, IReadOnlyList<string> Violations)
{
    public bool IsClean => Lost + Duplicated + Gaps + ObserversBehind + ProjectionsBehind + ReducersBehind + FailedPartitions + ReactorDuplicates == 0 && Violations.Count == 0;
}
