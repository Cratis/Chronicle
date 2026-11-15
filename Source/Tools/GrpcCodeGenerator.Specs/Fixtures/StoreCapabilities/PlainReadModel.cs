// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace TestAssembly.StoreCapabilities;

[ReadModel]
public record PlainReadModel(string Name)
{
    public static IEnumerable<PlainReadModel> ForStore(string eventStore) => [];
}
