// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents the first difference between a stored schema and an incoming one that is not tolerated.
/// </summary>
/// <param name="Path">The JSON path of the schema declaration holding the difference, for example "$/properties/name".</param>
/// <param name="Keyword">The schema keyword that differs, for example "type".</param>
public sealed record SchemaDifference(string Path, string Keyword);
