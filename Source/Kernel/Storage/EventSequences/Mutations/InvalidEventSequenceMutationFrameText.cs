// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// The exception that is thrown when text cannot be represented in a canonical event sequence mutation frame.
/// </summary>
/// <param name="field">The name of the malformed field.</param>
public class InvalidEventSequenceMutationFrameText(string field) : Exception($"The event sequence mutation frame field '{field}' is not valid strict UTF-8 text.");
