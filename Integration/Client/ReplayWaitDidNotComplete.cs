// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Represents the failure of a replay wait together with a bounded snapshot of the jobs and observers involved.
/// </summary>
/// <param name="snapshot">The bounded, allowlisted snapshot taken after the wait failed.</param>
/// <param name="original">The original wait failure, retained unchanged with its stack as the inner exception.</param>
internal sealed class ReplayWaitDidNotComplete(string snapshot, Exception original)
    : Exception($"Waiting for replay did not complete. Diagnostic snapshot, taken after the failure and not atomic with it:{Environment.NewLine}{snapshot}", original);
