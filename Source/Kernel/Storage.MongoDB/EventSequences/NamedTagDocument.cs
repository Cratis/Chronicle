// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents a named tag embedded in an event document.
/// </summary>
/// <param name="Name">The name of the tag.</param>
/// <param name="Value">The exact value of the tag.</param>
public record NamedTagDocument(string Name, string Value);
