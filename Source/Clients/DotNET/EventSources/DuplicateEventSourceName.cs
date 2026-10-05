// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// The exception that is thrown when more than one event source definition has the same name.
/// </summary>
/// <param name="name">The name that is shared.</param>
/// <param name="types">The types that share it.</param>
public class DuplicateEventSourceName(string name, IEnumerable<Type> types)
    : Exception($"The event source name '{name}' is declared by more than one type: {string.Join(", ", types.Select(_ => _.FullName))}.");
