// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Schema;

namespace Cratis.Extensions.Dolittle.Schemas
{
    /// <summary>
    /// Defines something that can provide schema metadata for a specific type.
    /// </summary>
    /// <remarks>
    /// An extension works on types, the type can be the event type itself or
    /// types used on events. <see cref="JSchema">JSON schemas</see> represents both,
    /// while an <see cref="EventSchema"/> represents an enclosing event.
    /// </remarks>
    public interface ICanExtendSchemaForType
    {
        /// <summary>
        /// Gets the type that schema information can be provided for.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Extend schema.
        /// </summary>
        /// <param name="eventSchema"><see cref="EventSchema"/> to provide for.</param>
        /// <param name="typeSchema">Specific <see cref="JSchema"/> for type to provide for.</param>
        void Extend(EventSchema eventSchema, JSchema typeSchema);
    }
}
