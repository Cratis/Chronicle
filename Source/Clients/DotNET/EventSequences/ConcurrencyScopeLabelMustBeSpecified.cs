// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when an event target or concurrency-scope label is unspecified, blank, or whitespace.
/// </summary>
public class ConcurrencyScopeLabelMustBeSpecified()
    : Exception("An event target or concurrency scope label must contain a non-whitespace value.");
