// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

internal sealed record RunOptions(int Seed, TimeSpan Duration, TimeSpan Settle, IReadOnlyList<FaultKind> Faults, string Output, bool KeepContainers, bool SelfTestViolation);
