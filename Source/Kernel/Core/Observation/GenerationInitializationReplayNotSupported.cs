// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Refuses a legacy replay that would conflict with projection-generation initialization.
/// </summary>
internal sealed class GenerationInitializationReplayNotSupported : ReplayNotSupported
{
    /// <summary>
    /// The reason a full replay cannot start.
    /// </summary>
    internal const string StartRefusedReason = "A full replay cannot start while projection-generation initialization is in progress or complete for this event store; the legacy replay would create work the initializer has to drain. No replay was started.";

    /// <summary>
    /// The reason a full replay cannot start when initialization progress could not be read.
    /// </summary>
    internal const string StartUnverifiedReason = "A full replay cannot start because the projection-generation initialization progress for this event store could not be read; starting it could create work the initializer has to drain. No replay was started.";

    /// <summary>
    /// The reason a persisted legacy replay cannot continue.
    /// </summary>
    internal const string ContinueRefusedReason = "A legacy replay cannot continue after generation 1 has adopted this event store's read models in place; completing it would swap the adopted container. No container was changed.";

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerationInitializationReplayNotSupported"/> class.
    /// </summary>
    /// <param name="reason">The explanation of the refusal.</param>
    /// <param name="innerException">The failure that caused the refusal, if any.</param>
    internal GenerationInitializationReplayNotSupported(string reason, Exception? innerException = null)
        : base(reason, innerException)
    {
    }
}
