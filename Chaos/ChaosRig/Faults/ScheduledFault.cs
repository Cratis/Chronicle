// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

internal sealed record ScheduledFault(TimeSpan At, FaultKind Kind, bool Resume = false);
