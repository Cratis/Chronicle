// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_converting_a_value_for_a_default_only_property : Specification
{
    JsonObject _result;

    void Because()
    {
        dynamic content = new ExpandoObject();
        content.delay = 120;
        content.duration = 7200;
        _result = new ExpandoObjectConverter(new TypeFormats()).ToJsonObject((ExpandoObject)content,
            JsonSchema.FromJson("""{"type":"object","properties":{"delay":{"default":null},"duration":{"default":null}}}"""));
    }

    [Fact] void should_keep_the_delay() => _result["delay"]!.GetValue<int>().ShouldEqual(120);
    [Fact] void should_keep_the_duration() => _result["duration"]!.GetValue<int>().ShouldEqual(7200);
}
