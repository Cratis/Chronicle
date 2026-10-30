// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.EventSequences.for_CanonicalJson;

public class when_encoding_an_object_with_properties_out_of_ordinal_order : Specification
{
    ExpandoObject _content;
    string _result;

    void Establish()
    {
        var nested = new ExpandoObject();
        var nestedValues = (IDictionary<string, object?>)nested;
        nestedValues["b"] = 2;
        nestedValues["a"] = 1;

        _content = new ExpandoObject();
        var values = (IDictionary<string, object?>)_content;
        values["zeta"] = "last";
        values["Zeta"] = "upper case sorts first ordinally";
        values["alpha"] = nested;
        values["ålpha"] = "outside ASCII sorts after it";
        values["_under"] = new object[] { 3, 1, 2 };
    }

    void Because() => _result = CanonicalJson.Encode(_content);

    [Fact] void should_write_the_properties_in_ordinal_order_at_every_level_and_keep_array_order() =>
        _result.ShouldEqual("{\"Zeta\":\"upper case sorts first ordinally\",\"_under\":[3,1,2],\"alpha\":{\"a\":1,\"b\":2},\"zeta\":\"last\",\"ålpha\":\"outside ASCII sorts after it\"}");
}
