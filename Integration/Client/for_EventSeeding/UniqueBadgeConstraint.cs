// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.for_EventSeeding;

public class UniqueBadgeConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .Unique(b => b.On<BadgeIssued>(e => e.Badge));
}
