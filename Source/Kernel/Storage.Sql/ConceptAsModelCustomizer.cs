// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Model customizer that configures value converters for ConceptAs types.
/// </summary>
public class ConceptAsModelCustomizer : RelationalModelCustomizer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConceptAsModelCustomizer"/> class.
    /// </summary>
    /// <param name="dependencies">The model customizer dependencies.</param>
    public ConceptAsModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <inheritdoc/>
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        // Apply ConceptAs value converters to all properties
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var propertyType = property.ClrType;
                if (propertyType.IsConcept())
                {
                    var valueType = propertyType.GetConceptValueType();
                    var converterType = typeof(ConceptAsValueConverter<,>).MakeGenericType(propertyType, valueType);
                    var converter = (ValueConverter)Activator.CreateInstance(converterType, new object?[] { null })!;

                    property.SetValueConverter(converter);
                }
            }
        }
    }
}
