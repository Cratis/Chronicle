// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents metadata about a type format.
/// </summary>
/// <param name="JsonType">The JSON schema type (string, number, integer, boolean).</param>
/// <param name="ClrType">The CLR type.</param>
/// <param name="Format">The format identifier.</param>
public record TypeFormatMetadata(string JsonType, Type ClrType, string Format);
