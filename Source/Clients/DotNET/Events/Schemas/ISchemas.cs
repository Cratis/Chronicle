// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Schemas
{
    /// <summary>
    /// Defines a system for working with event schemas from the client perspective.
    /// </summary>
    public interface ISchemas
    {
        /// <summary>
        /// Registers schemas for all event types.
        /// </summary>
        void RegisterAll();
    }
}
