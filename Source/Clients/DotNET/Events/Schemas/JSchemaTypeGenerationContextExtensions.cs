// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace Cratis.Events.Schemas
{
    /// <summary>
    /// Holds extension methods for working with <see cref="JSchemaTypeGenerationContext"/>.
    /// </summary>
    public static class JSchemaTypeGenerationContextExtensions
    {
        /// <summary>
        /// Get a schema with format information for supported known types - return null if no schema created.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="type"></param>
        /// <returns>New <see cref="JSchema"/> or null if type not supported for format info.</returns>
        public static JSchema GetFormatSchemaFor(this JSchemaTypeGenerationContext context, Type type)
        {
            var typesFormatInfo = new Dictionary<Type, string>
            {
                { typeof(int), "int32" },
                { typeof(uint), "uint32" },
                { typeof(long), "int64" },
                { typeof(ulong), "uint64" },
                { typeof(float), "float" },
                { typeof(double), "double" },
                { typeof(decimal), "decimal" },
                { typeof(byte), "byte" },
                { typeof(DateTime), "date-time" },
                { typeof(DateTimeOffset), "date-time" },
            };

            if (!typesFormatInfo.ContainsKey(type)) return null!;

            var generator = new JSchemaGenerator();
            var schema = generator.Generate(type, context.Required != Required.Always);
            schema.Format = typesFormatInfo[type];
            return schema;
        }
    }
}
