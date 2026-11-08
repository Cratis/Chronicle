// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// The exception that is thrown when an import cannot be validated because a constraint index stays unavailable.
/// </summary>
/// <param name="importId">The <see cref="ImportId"/>.</param>
public class ImportValidationUnavailable(ImportId importId)
    : Exception($"Import '{importId}' could not be validated because a constraint index it depends on stayed unavailable. Nothing was committed.");
