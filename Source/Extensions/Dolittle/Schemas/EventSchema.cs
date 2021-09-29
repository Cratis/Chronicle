// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Schema;

namespace Cratis.Extensions.Dolittle.Schemas
{
    /// <summary>
    /// Represents the schema of an event.
    /// </summary>
    /// <param name="EventType">The <see cref="EventType"/> represented.</param>
    /// <param name="Schema">The underlying <see cref="JSchema"/>.</param>
    public record EventSchema(global::Dolittle.SDK.Events.EventType EventType, JSchema Schema);
}
