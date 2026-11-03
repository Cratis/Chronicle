// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelInstanceExtensions.when_removing_sink_bookkeeping;

public class and_there_is_no_bookkeeping : Specification
{
    ExpandoObject _instance;
    ExpandoObject _result;

    void Establish()
    {
        _instance = new ExpandoObject();
        ((IDictionary<string, object?>)_instance)["name"] = "the name";
    }

    void Because() => _result = _instance.WithoutSinkBookkeeping(new JsonSchema());

    [Fact] void should_return_the_same_instance() => _result.ShouldBeSame(_instance);
}
