// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public record StreamedEntry(Guid Id, string Name, int Count, DateTime Created, string? Note, List<string> Tags, Dictionary<string, string> Labels);
