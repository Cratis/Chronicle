// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// The exception thrown for invalid event sequence page bounds or size.
/// </summary>
/// <param name="message">The validation failure.</param>
public class InvalidEventSequencePage(string message) : Exception(message);
