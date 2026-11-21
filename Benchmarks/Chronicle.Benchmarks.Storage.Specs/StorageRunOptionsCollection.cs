// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Serializes specs that mutate the global storage run options.</summary>
[CollectionDefinition(nameof(StorageRunOptionsCollection), DisableParallelization = true)]
public class StorageRunOptionsCollection;
