// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Aksio.Cratis.Json
{
    /// <summary>
    /// Extension methods for working with <see cref="JsonObject"/>.
    /// </summary>
    public static class JsonObjectExtensions
    {
        /// <summary>
        /// Deep clones a <see cref="JsonObject"/> for a new instance.
        /// </summary>
        /// <param name="json"><see cref="JsonObject"/> to clone.</param>
        /// <returns>A new <see cref="JsonObject"/> instance.</returns>
        public static JsonObject DeepClone(this JsonObject json)
        {
            return (JsonNode.Parse(json.ToJsonString()) as JsonObject)!;
        }
    }
}
