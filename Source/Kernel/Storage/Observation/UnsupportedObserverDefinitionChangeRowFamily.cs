// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// The exception that is thrown when an unsupported payload is used for definition-change rows.
/// </summary>
/// <param name="type">The unsupported payload type.</param>
public class UnsupportedObserverDefinitionChangeRowFamily(Type type) : Exception($"'{type}' is not a definition-change row family.");
