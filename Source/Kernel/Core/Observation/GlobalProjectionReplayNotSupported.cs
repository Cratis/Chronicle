// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Refuses a namespace-local operation on an uncoordinated global replay.
/// </summary>
internal sealed class GlobalProjectionReplayNotSupported : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalProjectionReplayNotSupported"/> class.
    /// </summary>
    internal GlobalProjectionReplayNotSupported()
        : base(GlobalProjectionReplay.RefusalReason)
    {
    }
}
