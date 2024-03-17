// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Json.for_ConceptAsJsonConverter.given;

public abstract class converter_for_converting_to_json<TConcept, TUnderlying> : Specification
{
    protected ConceptAsJsonConverter<TConcept> converter;
    protected TConcept input;
    protected string result;
    MemoryStream stream;
    Utf8JsonWriter writer;

    protected abstract TUnderlying Expected { get; }
    protected abstract string FormattedExpected { get; }

    void Establish()
    {
        converter = new();

        input = (TConcept)typeof(TConcept)
                    .GetConstructors()[0]
                    .Invoke(new object[] { Expected });
        stream = new();
        writer = new(stream);
    }

    void Because()
    {
        converter.Write(writer, input, default);
        writer.Flush();
        result = Encoding.UTF8.GetString(stream.ToArray());
    }

    protected void ShouldConvertToJson() => result.ShouldEqual(FormattedExpected);
}
