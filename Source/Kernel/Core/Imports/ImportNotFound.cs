// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// The exception that is thrown when an import that is being committed does not exist.
/// </summary>
/// <param name="importId">The <see cref="ImportId"/>.</param>
public class ImportNotFound(ImportId importId) : Exception($"Import '{importId}' does not exist.");
