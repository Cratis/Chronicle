// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Json.for_TypeWithObjectPropertiesJsonConverter;

public class Converter : TypeWithObjectPropertiesJsonConverter<TypeWithObjectProperties>
{
    protected override IEnumerable<string> ObjectProperties => new[] {
        nameof(TypeWithObjectProperties.FirstObjectProperty),
        nameof(TypeWithObjectProperties.SecondObjectProperty)
    };
}
