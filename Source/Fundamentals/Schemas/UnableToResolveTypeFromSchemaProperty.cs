// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Aksio.Cratis.Schemas;

/// <summary>
/// Exception that gets thrown when not able to resolve type from a <see cref="JsonSchemaProperty"/>.
/// </summary>
public class UnableToResolveTypeFromSchemaProperty : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnableToResolveTypeFromSchemaProperty"/>.
    /// </summary>
    /// <param name="property"><see cref="JsonSchemaProperty"/> that is wrong.</param>
    public UnableToResolveTypeFromSchemaProperty(JsonSchemaProperty property) : base($"Unable to resolve type for property '{property.Name}'")
    {
    }
}
