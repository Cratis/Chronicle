// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.given;

public class a_concurrency_validator : Specification
{
    protected ConcurrencyValidator _validator;
    protected IEventSequenceStorage _eventSequenceStorage;

    void Establish()
    {
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _validator = new ConcurrencyValidator(_eventSequenceStorage);
    }
}
