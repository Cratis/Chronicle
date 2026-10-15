// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

public class ClosingStream : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder.ClosesStreamOn<StreamClosed>(
        scope => scope.PerEventSourceId().PerEventStreamType().PerEventStreamId().ReopenedBy<StreamReopened>(),
        nameof(ClosingStream));
}
