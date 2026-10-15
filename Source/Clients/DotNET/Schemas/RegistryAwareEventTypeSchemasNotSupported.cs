// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// The exception that is thrown when a custom generator cannot bind compensation metadata to explicit event identities.
/// </summary>
/// <param name="generatorType">The custom generator type.</param>
/// <param name="eventType">The event type requiring registry-aware generation.</param>
public class RegistryAwareEventTypeSchemasNotSupported(Type generatorType, Type eventType)
    : Exception($"'{generatorType.FullName}' must implement registry-aware schema generation for '{eventType.FullName}' because its compensation target requires an explicit event identity.");
