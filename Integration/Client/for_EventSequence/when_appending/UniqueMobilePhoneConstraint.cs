// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

public class UniqueMobilePhoneConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .Unique(b => b.On<PersonRegistered>(e => e.Mobile.CountryPrefix, e => e.Mobile.Number));
}
