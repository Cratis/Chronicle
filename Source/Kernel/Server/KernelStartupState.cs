// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// Tracks readiness after every StartedAsync, including the silo's Active lifecycle stage, has completed.
/// </summary>
/// <param name="lifetime">The host application lifetime.</param>
internal sealed class KernelStartupState(IHostApplicationLifetime lifetime)
{
    /// <summary>
    /// Gets whether startup has completed and shutdown has not begun.
    /// </summary>
    internal bool IsReady => lifetime.ApplicationStarted.IsCancellationRequested && !lifetime.ApplicationStopping.IsCancellationRequested;
}
