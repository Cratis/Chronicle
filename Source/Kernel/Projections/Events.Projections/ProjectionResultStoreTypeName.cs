// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents a friendly name for a type of <see cref="IProjectionResultStore"/>
    /// </summary>
    /// <param name="Value">Underlying value.</param>
    public record ProjectionResultStoreTypeName(string Value) : ConceptAs<string>(Value)
    {
        /// <summary>
        /// Implicitly convert from <see cref="string"/>  to <see cref="ProjectionResultStoreTypeId"/>.
        /// </summary>
        /// <param name="value">String value to convert from.</param>
        public static implicit operator ProjectionResultStoreTypeName(string value) => new(value);
    }

}
