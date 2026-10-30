// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.EventSequences.for_CanonicalJson;

public class when_encoding_an_object_already_in_ordinal_order : Specification
{
    ExpandoObject _content;
    string _result;

    void Establish()
    {
        _content = new ExpandoObject();
        var values = (IDictionary<string, object?>)_content;
        values["a"] = 1;
        values["b"] = "two";
        values["c"] = null;
    }

    void Because() => _result = CanonicalJson.Encode(_content);

    [Fact] void should_write_the_properties_as_they_are() => _result.ShouldEqual("{\"a\":1,\"b\":\"two\",\"c\":null}");
}
