// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_DateTimeValues;

public class when_holding_content_of_a_recursive_schema : Specification
{
    const string Definition = """
        {
            "type": "object",
            "properties": {
                "Name": { "type": "string" },
                "Next": { "$ref": "#/definitions/Node" }
            }
        }
        """;

    JsonSchema _schema;
    ExpandoObject _content;
    ExpandoObject _result;
    bool _completed;

    void Establish()
    {
        _schema = JsonSchema.FromJson($$"""
            {
                "type": "object",
                "properties": {
                    "Name": { "type": "string" },
                    "Next": { "$ref": "#/definitions/Node" }
                },
                "definitions": { "Node": {{Definition}} }
            }
            """);
        _content = new ExpandoObjectConverter(new TypeFormats()).ToExpandoObject(
            Encoding.UTF8.GetBytes("""{ "Name": "root", "Next": { "Name": "a", "Next": { "Name": "b" } } }"""),
            _schema);
    }

    void Because()
    {
        var thread = new Thread(() => _result = DateTimeValues.AsHeldBeforeOffsetsWereKept(_content, _schema), 256 * 1024);
        thread.Start();
        _completed = thread.Join(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_complete() => _completed.ShouldBeTrue();
    [Fact] void should_leave_the_content_as_it_is() => _result.ShouldEqual(_content);
}
