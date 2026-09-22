// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Exception thrown when a calendar value (ISO week, etc.) cannot be derived from a value type.
/// </summary>
/// <param name="valueType">The type of the value that could not be converted to DateTimeOffset.</param>
public class UnableToDeriveCalendarValue(Type valueType)
    : Exception($"Unable to derive a calendar value from type '{valueType.Name}'. Supported types: DateTimeOffset, DateTime, DateOnly, string.");
