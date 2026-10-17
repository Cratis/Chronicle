// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Occurrences;

/// <summary>
/// The exception that is thrown when a synthetic context cannot acquire its intended lease.
/// </summary>
public sealed class OccurrenceContextCannotBeClaimed() : Exception("A context requires positive attempt and epoch values and an empty trigger queue before recording and claiming.");
