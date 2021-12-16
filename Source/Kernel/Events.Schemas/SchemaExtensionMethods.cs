// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Newtonsoft.Json.Schema;

namespace Cratis.Events.Schemas
{
    /// <summary>
    /// Extension methods for working with <see cref="JSchema"/> and specific metadata.
    /// </summary>
    public static class SchemaExtensionMethods
    {
        const string DisplayNameExtension = "displayName";
        const string GenerationExtension = "generation";

        /// <summary>
        /// Set the display name for a schema.
        /// </summary>
        /// <param name="schema"><see cref="JSchema"/> to use.</param>
        /// <param name="name">Name to set.</param>
        public static void SetDisplayName(this JSchema schema, string name)
        {
            schema.ExtensionData[DisplayNameExtension] = name;
        }

        /// <summary>
        /// Set the generation for a schema.
        /// </summary>
        /// <param name="schema"><see cref="JSchema"/> to use.</param>
        /// <param name="generation">Generation to set.</param>
        public static void SetGeneration(this JSchema schema, uint generation)
        {
            schema.ExtensionData[GenerationExtension] = generation;
        }

        /// <summary>
        /// Get the display name for a schema.
        /// </summary>
        /// <param name="schema"><see cref="JSchema"/> to use.</param>
        /// <returns>Name.</returns>
        public static string GetDisplayName(this JSchema schema)
        {
            return schema.ExtensionData[DisplayNameExtension].ToString();
        }

        /// <summary>
        /// Get the generation for a schema.
        /// </summary>
        /// <param name="schema"><see cref="JSchema"/> to use.</param>
        /// <returns>Generation.</returns>
        public static uint GetGeneration(this JSchema schema)
        {
            return uint.Parse(schema.ExtensionData[GenerationExtension].ToString(), CultureInfo.InvariantCulture);
        }
    }
}
