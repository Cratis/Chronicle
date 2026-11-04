// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Decides the name of the primary-key column of a read model table, using the spelling the schema itself uses.
/// </summary>
internal static class ReadModelKeyNames
{
    const string PascalCase = "Id";
    const string CamelCase = "id";

    /// <summary>
    /// Gets the name of the key column for a schema.
    /// </summary>
    /// <param name="schema">The read model schema.</param>
    /// <returns>
    /// <c language="csharp">Id</c> or <c language="csharp">id</c> when the schema declares one of them (<c language="csharp">Id</c> wins),
    /// otherwise the schema's own spelling of the identifier (for example <c language="csharp">ID</c>), otherwise the likely
    /// key name, which the table then synthesizes.
    /// </returns>
    public static string For(JsonSchema schema)
    {
        if (schema.Properties.ContainsKey(PascalCase))
        {
            return PascalCase;
        }

        if (schema.Properties.ContainsKey(CamelCase))
        {
            return CamelCase;
        }

        var spelled = schema.Properties.Keys.FirstOrDefault(name => string.Equals(name, CamelCase, StringComparison.OrdinalIgnoreCase));
        return spelled ?? schema.GetLikelyKeyPropertyName() ?? PascalCase;
    }

    /// <summary>
    /// Gets whether a table for the schema may already exist in the layout earlier releases created, so the sink has to look at it.
    /// </summary>
    /// <param name="schema">The read model schema.</param>
    /// <returns>True when the schema spells its identifier other than <c language="csharp">Id</c> or <c language="csharp">id</c>.</returns>
    /// <remarks>
    /// Earlier releases ignored such an identifier, kept it as a nullable column and synthesized an <c language="csharp">Id</c> key.
    /// </remarks>
    public static bool RequiresLayoutResolution(JsonSchema schema)
    {
        var name = For(schema);
        return name is not (PascalCase or CamelCase) && schema.Properties.ContainsKey(name);
    }
}
