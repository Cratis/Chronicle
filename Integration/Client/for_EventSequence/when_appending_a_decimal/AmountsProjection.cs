// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_a_decimal;

public class AmountsProjection : IProjectionFor<Amounts>
{
    public void Define(IProjectionBuilderFor<Amounts> builder) => builder.AutoMap().From<AmountRecorded>();
}
