// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.InProcess.Integration.for_EventAppendCollection;

public class ADirectUniqueFollowUpConstraint : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(b => b.On<ADirectUniqueFollowUpEvent>(e => e.UniqueValue));
}
