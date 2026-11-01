// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Integration.Clustering.for_JobsRolePlacement;

public class JobStepPlacementSignal
{
    bool _capturing;

    public ConcurrentDictionary<GrainId, SiloAddress> Steps { get; } = new();

    public bool Capturing
    {
        get => Volatile.Read(ref _capturing);
        set => Volatile.Write(ref _capturing, value);
    }
}
