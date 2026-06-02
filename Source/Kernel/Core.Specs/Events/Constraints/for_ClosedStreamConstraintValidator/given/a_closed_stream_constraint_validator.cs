// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ClosedStreamConstraintValidator.given;

public abstract class a_closed_stream_constraint_validator : Specification
{
    protected ClosedStreamConstraintValidator _validator;
    protected IClosedStreamsConstraintStorage _storage;

    void Establish()
    {
        _storage = Substitute.For<IClosedStreamsConstraintStorage>();
        _validator = new ClosedStreamConstraintValidator(_storage);
    }
}
