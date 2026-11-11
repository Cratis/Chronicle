// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// Represents how a run of the kernel host ended, with the evidence it was decided from.
/// </summary>
/// <param name="Outcome">The <see cref="KernelRunOutcome"/>.</param>
/// <param name="Failure">The exception the run ended with, or <see langword="null"/> when nothing failed.</param>
/// <param name="ShutdownTimeout">The host shutdown timeout the stop ran against.</param>
/// <param name="StopDuration">How long the stop took, or <see cref="TimeSpan.Zero"/> when the host did not stop.</param>
internal sealed record KernelRunResult(KernelRunOutcome Outcome, Exception? Failure, TimeSpan ShutdownTimeout, TimeSpan StopDuration);
