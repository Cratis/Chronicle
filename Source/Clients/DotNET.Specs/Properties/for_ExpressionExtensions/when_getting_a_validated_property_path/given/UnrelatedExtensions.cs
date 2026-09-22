// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_ExpressionExtensions.when_getting_a_validated_property_path.given;

public static class UnrelatedExtensions
{
    public static int NotADerivedFunction(this DateTimeOffset dateTimeOffset) => dateTimeOffset.Year;
}
