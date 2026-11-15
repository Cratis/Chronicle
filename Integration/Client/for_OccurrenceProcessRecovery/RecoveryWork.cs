// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Integration.for_OccurrenceProcessRecovery;

[Trigger("process-recovery-work")]
public record RecoveryWork(string Value);
