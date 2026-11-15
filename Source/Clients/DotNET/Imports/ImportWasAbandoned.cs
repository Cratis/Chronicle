// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports;

/// <summary>
/// The exception that is thrown when events are added to, or an attempt is made to complete, an import that was abandoned.
/// </summary>
/// <param name="importId">The <see cref="ImportId"/>.</param>
public class ImportWasAbandoned(ImportId importId) : Exception($"Import '{importId}' was abandoned; no more events are sent to it and it cannot be completed.");
