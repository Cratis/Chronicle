// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace TestAssembly.StoreCapabilities;

[ReadModel]
[Cratis.Chronicle.Grpc.StoreCapability("chronicle.test.read")]
public record CapabilityReadModel(string Name)
{
    public static IEnumerable<CapabilityReadModel> ForStore(string eventStore) => [];
}
