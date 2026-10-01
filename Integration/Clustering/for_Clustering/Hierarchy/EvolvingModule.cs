// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering.Hierarchy;

public record EvolvingModule(Guid Id, string Name, string Label, IEnumerable<EvolvingFeature> Features);
