// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Json.for_JsonElementExtensions;

public abstract class when_trying_to_get_value_of_type<T> : Specification
{
    protected bool result;
    protected T output;
    protected abstract T expected { get; }

    void Because()
    {
        var document = typeof(T).IsPrimitive ?
            JsonDocument.Parse($"{{\"TheValue\":{expected}}}") :
            JsonDocument.Parse($"{{\"TheValue\":\"{expected}\"}}");
        var element = document.RootElement.EnumerateObject().First().Value;
        result = element.TryGetValue(out var theOutput);
        output = (T)theOutput!;
    }
}
