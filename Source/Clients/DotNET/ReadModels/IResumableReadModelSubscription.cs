// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines a read model subscription that can re-establish its stream once the event store has registered again.
/// </summary>
internal interface IResumableReadModelSubscription : IDisposable
{
    /// <summary>
    /// Re-establishes the stream if it was lost, reporting a failure instead of throwing it.
    /// </summary>
    void Resume();
}
