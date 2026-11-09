// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// The exception that is thrown when a dictionary key resolved from an event cannot be stored as a key.
/// </summary>
/// <remarks>
/// A key cannot be empty, cannot contain '.', '[' or ']' - they separate the segments of a property path - cannot contain
/// a null character and cannot start with '$', which marks an operator in MongoDB. The key is not escaped silently; the
/// event fails and its partition shows the failure.
/// </remarks>
/// <param name="propertyPath">The <see cref="PropertyPath"/> holding the dynamic key segment.</param>
/// <param name="key">The key resolved from the event.</param>
public class InvalidDictionaryKey(PropertyPath propertyPath, string key)
    : Exception($"The dictionary key '{key}' resolved for '{propertyPath}' cannot be used as a key - a key cannot be empty, cannot contain '.', '[', ']' or a null character and cannot start with '$'.");
