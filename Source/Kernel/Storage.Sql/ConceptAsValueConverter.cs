// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Generic value converter for ConceptAs types.
/// </summary>
/// <typeparam name="TConcept">The ConceptAs type.</typeparam>
/// <typeparam name="TValue">The underlying primitive type.</typeparam>
/// <param name="mappingHints">Mapping hints for the converter.</param>
public class ConceptAsValueConverter<TConcept, TValue>(ConverterMappingHints? mappingHints = null) : ValueConverter<TConcept, TValue>(
    concept => ExtractValue(concept),
    value => CreateConcept(value),
    mappingHints)
    where TConcept : class
{
    static TValue ExtractValue(TConcept concept)
    {
        var valueProperty = typeof(TConcept).GetProperty("Value");
        return (TValue)valueProperty!.GetValue(concept)!;
    }

    static TConcept CreateConcept(TValue value)
    {
        // ConceptAs types have a constructor that takes the value
        return (TConcept)Activator.CreateInstance(typeof(TConcept), value)!;
    }
}
