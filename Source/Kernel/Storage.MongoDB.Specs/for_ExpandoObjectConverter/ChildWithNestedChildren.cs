// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public record ChildWithNestedChildren(
    Guid ConfigurationId,
    string Name,
    double Distance,
    IEnumerable<GrandChild> Hubs);
