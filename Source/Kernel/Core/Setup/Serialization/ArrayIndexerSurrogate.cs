// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries an <see cref="ArrayIndexer"/> between grains. The type is shared with the clients and does not reference Orleans.
/// </summary>
/// <param name="ArrayProperty">The <see cref="PropertyPath"/> of the array.</param>
/// <param name="IdentifierProperty">The <see cref="PropertyPath"/> of the identifier within an item.</param>
/// <param name="Identifier">The identifier value.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.ArrayIndexerSurrogate")]
public readonly record struct ArrayIndexerSurrogate(
    [property: Id(0)] PropertyPath ArrayProperty,
    [property: Id(1)] PropertyPath IdentifierProperty,
    [property: Id(2)] object Identifier);
