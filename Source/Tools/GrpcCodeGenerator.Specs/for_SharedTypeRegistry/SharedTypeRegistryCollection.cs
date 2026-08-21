// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

/// <summary>
/// Forces every <see cref="SharedTypeRegistry"/> spec to run sequentially rather than in xUnit's default parallel
/// classes. The registry is deliberately static, global, mutable state - correct for the single-shot CLI process
/// it actually runs in, but two spec classes mutating and enumerating it at the same time is a data race the real
/// generator never has, not a defect the specs should be papering over.
/// </summary>
[CollectionDefinition(Name)]
public static class SharedTypeRegistryCollection
{
    /// <summary>
    /// Gets the name of the collection.
    /// </summary>
    public const string Name = "SharedTypeRegistry";
}
