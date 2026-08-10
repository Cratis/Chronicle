// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.when_converting_non_compliant_string_scalars;

public class and_it_names_an_enum_member : given.a_converter_with_scalar_schema
{
    ExpandoObject _result;

    void Because() => _result = Convert("status", "Verified");

    [Fact] void should_preserve_enum_name_mapping() => ((IDictionary<string, object?>)_result)["status"].ShouldEqual(1);
}
