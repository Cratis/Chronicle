// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// The exception that is thrown when sequencing key text is not well-formed UTF-16.
/// </summary>
/// <param name="reason">The reason the text is invalid.</param>
public sealed class InvalidSequencingKeyText(string reason) : Exception($"Invalid sequencing key text: {reason}");
