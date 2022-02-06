// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties
{
    /// <summary>
    /// Represents information on how to index an array.
    /// </summary>
    /// <param name="ArrayIndex">Identifier of the <see cref="Properties.ArrayIndex"/>.</param>
    /// <param name="Property"><see cref="PropertyPath"/> within the object that holds the identifying value.</param>
    /// <param name="Identifier">The identifying value.</param>
    public record ArrayIndexer(string ArrayIndex, PropertyPath Property, object Identifier);
}
