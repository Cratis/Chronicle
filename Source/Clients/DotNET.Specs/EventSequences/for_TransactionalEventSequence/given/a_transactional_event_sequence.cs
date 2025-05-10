// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_TransactionalEventSequence.when_appending.given;

public class a_transactional_event_sequence : Specification
{
    protected TransactionalEventSequence _transactionalEventSequence;
    protected IEventSequence _eventSequence;
    protected IUnitOfWorkManager _unitOfWorkManager;
    protected IUnitOfWork _unitOfWork;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _eventSequence.Id.Returns(EventSequenceId.Log);
        _unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWorkManager.Current.Returns(_unitOfWork);
        _transactionalEventSequence = new TransactionalEventSequence(_eventSequence, _unitOfWorkManager);
    }
}
