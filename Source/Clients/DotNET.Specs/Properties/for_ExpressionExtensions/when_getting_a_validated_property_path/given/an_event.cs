// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_ExpressionExtensions.when_getting_a_validated_property_path.given;

public class an_event : Specification
{
    public record Owner(string Name);

    public record SomeEvent(string Name, Owner Owner, int Age, bool Active, string Fallback);
}
