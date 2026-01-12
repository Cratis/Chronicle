// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.TypeFormats;

/// <summary>
/// Represents a type format.
/// </summary>
/// <param name="JsonType">The JSON schema type.</param>
/// <param name="ClrTypeName">Name of the CLR type.</param>
/// <param name="Format">The format string.</param>
public record TypeFormat(string JsonType, string ClrTypeName, string Format);
