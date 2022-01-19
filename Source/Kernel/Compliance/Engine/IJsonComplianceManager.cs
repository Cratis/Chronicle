// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Aksio.Cratis.Compliance
{
    /// <summary>
    /// Defines a Json serializer that is compliance aware.
    /// </summary>
    public interface IJsonComplianceManager
    {
        /// <summary>
        /// Apply compliance rules to JSON.
        /// </summary>
        /// <param name="schema"><see cref="JsonSchema"/> that represents the object.</param>
        /// <param name="identifier">Identifier of the object.</param>
        /// <param name="json">JSON to apply rules for.</param>
        /// <returns>Compliance approved JSON.</returns>
        Task<JObject> Apply(JsonSchema schema, string identifier, JObject json);

        /// <summary>
        /// Release JSON from compliance rules.
        /// </summary>
        /// <param name="schema"><see cref="JsonSchema"/> that represents the object.</param>
        /// <param name="identifier">Identifier of the object.</param>
        /// <param name="json">JSON to release rules for.</param>
        /// <returns>Released version of the JSON.</returns>
        Task<JObject> Release(JsonSchema schema, string identifier, JObject json);
    }
}
