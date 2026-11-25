// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries <see cref="ArrayIndexers"/> between grains. The type is shared with the clients and does not reference Orleans.
/// </summary>
/// <param name="All">The <see cref="ArrayIndexer">indexers</see>.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.ArrayIndexersSurrogate")]
public readonly record struct ArrayIndexersSurrogate([property: Id(0)] IReadOnlyList<ArrayIndexer> All);
