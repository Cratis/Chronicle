// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public record WithNestedProperties(string Title, NestedChild? Child, IEnumerable<NestedChild> Children, Guid Id, IEnumerable<int> Numbers, string? Nothing);
