// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Json;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents a <see cref="JsonConverter"/> that can convert to a dictionary of <see cref="PropertyPath"/> and <see cref="ChildrenDefinition"/>.
    /// </summary>
    public class FromDefinitionsConverter : DictionaryJsonConverter<EventType, FromDefinition>
    {
        /// <inheritdoc/>
        protected override EventType GetKeyFromString(string key) => new(key, 1);

        /// <inheritdoc/>
        protected override string GetKeyString(EventType key) => key.Id.ToString();
    }
}
