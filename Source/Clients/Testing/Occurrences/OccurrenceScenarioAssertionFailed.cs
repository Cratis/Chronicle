// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Occurrences;

/// <summary>
/// The exception that is thrown when collected occurrence scenario state does not meet an expectation.
/// </summary>
/// <param name="message">The unmet expectation.</param>
public sealed class OccurrenceScenarioAssertionFailed(string message) : Exception(message);
