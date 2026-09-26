// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when an event sequence implementation cannot carry structured named tags.
/// </summary>
/// <param name="implementation">The implementation that cannot carry named tags.</param>
public class NamedTagsNotSupported(Type implementation)
    : Exception($"Event sequence implementation '{implementation.FullName}' does not support named tags.");
