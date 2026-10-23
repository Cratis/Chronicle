// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_RolePlacement;

public interface IPlacementCaller : IGrainWithGuidKey
{
    Task<SiloAddress> Activate(string observerKey, string eventSequenceKey);
}
