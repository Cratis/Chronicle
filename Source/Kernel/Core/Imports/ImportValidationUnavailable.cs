// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// The exception that is thrown when an import cannot be validated because a constraint index it depends on, or the
/// event sequence it targets, stayed unavailable.
/// </summary>
/// <param name="importId">The <see cref="ImportId"/>.</param>
/// <param name="lastError">The optional error of the last attempt, when a call failed.</param>
public class ImportValidationUnavailable(ImportId importId, Exception? lastError = null)
    : Exception(
        $"Import '{importId}' could not be validated because a constraint index it depends on, or the event sequence it targets, stayed unavailable{(lastError is null ? string.Empty : $": {lastError.Message}")}. Nothing was committed.",
        lastError);
