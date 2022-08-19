// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Aksio.Cratis.Concepts;

namespace Aksio.Cratis.Json.for_ConceptAsJsonConverter.given;

public abstract class converter_for_converting_from_json<TConcept, TUnderlying> : Specification
{
    protected ConceptAsJsonConverter<TConcept> converter;
    protected TUnderlying input;
    protected TConcept result;

    protected abstract TUnderlying InputValue { get; }
    protected abstract string FormattedInput { get; }

    void Establish()
    {
        converter = new();
        input = InputValue;
    }

    void Because()
    {
        var input = FormattedInput;
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(input).AsSpan());
        if (input.StartsWith('"'))
        {
            reader.Read();  // Skip quote
        }
        result = converter.Read(ref reader, typeof(TConcept), default);
    }

    protected void ShouldConvertToCorrectConcept() => result.GetConceptValue().ShouldEqual(input);
}
