// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Represents information on how to index an array.
/// </summary>
/// <param name="ArrayProperty">Fully qualified identifier of the <see cref="Properties.ArrayProperty"/> within an object structure.</param>
/// <param name="IdentifierProperty"><see cref="PropertyPath"/> within the object that holds the identifying value.</param>
/// <param name="Identifier">The identifying value.</param>
public record ArrayIndexer(PropertyPath ArrayProperty, PropertyPath IdentifierProperty, object Identifier);
