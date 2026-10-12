// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// The exception that is thrown when stored sequencing key identity columns disagree.
/// </summary>
/// <param name="column">The inconsistent identity column.</param>
public sealed class SequencingKeyIdentityMismatch(string column) : Exception($"Stored sequencing key identity has an inconsistent {column}.");
