// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SharedTypeCatalog;

#pragma warning disable CA1819 // This fixture verifies generation for array-valued properties.
public record CollectionPayload(
    IEnumerable<string> Values,
    ICollection<string> Collection,
    IList<IList<string>> NestedLists,
    IReadOnlyList<string> ReadOnlyList,
    IReadOnlyCollection<string> ReadOnlyCollection,
    IDictionary<string, string> Dictionary,
    IReadOnlyDictionary<string, string> ReadOnlyDictionary,
    ISet<string> Set,
    IReadOnlySet<string> ReadOnlySet,
    string[] Array);
