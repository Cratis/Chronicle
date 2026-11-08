// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Imports;

/// <summary>
/// Represents a named tag of a staged import event.
/// </summary>
/// <param name="Name">The tag name.</param>
/// <param name="Value">The tag value.</param>
public record StagedImportNamedTag(string Name, string Value);
