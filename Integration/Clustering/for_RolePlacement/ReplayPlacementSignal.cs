// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

public class ReplayPlacementSignal
{
    public const string ProjectionId = "replay-placement";

    public SiloAddress JobsSilo { get; set; } = SiloAddress.Zero;
    public bool Capturing { get; set; }
    public ConcurrentDictionary<GrainId, SiloAddress> Steps { get; } = new();
    public ConcurrentDictionary<GrainId, SiloAddress> Subscribers { get; } = new();
}
