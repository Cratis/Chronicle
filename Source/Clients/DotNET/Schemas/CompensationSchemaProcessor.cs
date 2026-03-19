// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using NJsonSchema.Generation;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents an implementation of <see cref="ISchemaProcessor"/> for adding event compensation metadata.
/// </summary>
public class CompensationSchemaProcessor : ISchemaProcessor
{
    /// <inheritdoc/>
    public void Process(SchemaProcessorContext context)
    {
        var compensationAttribute = context.ContextualType.Type.GetCustomAttribute<CompensationForAttribute>();
        if (compensationAttribute is null)
        {
            return;
        }

        var compensatedEventType = compensationAttribute.CompensatedEventType.GetEventType();
        context.Schema.SetCompensationFor(compensatedEventType.Id.Value);
    }
}
