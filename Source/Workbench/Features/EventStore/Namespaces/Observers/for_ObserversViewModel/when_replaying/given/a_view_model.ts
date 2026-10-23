// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon, { SinonStubbedInstance } from 'sinon';
import { ObserversViewModel } from '../../../ObserversViewModel';
import { ClearObserverQuarantine, FastForward, RemoveObserver, ReplayObserver as Replay } from 'Features/Observation';
import { FastForwardResult, ObserverRemovalOutcome, ObserverRemovalResult } from 'Features/Concepts/Observation';
import { Dialogs } from '@cratis/arc.react.mvvm/dialogs';
import { type EventStoreAndNamespaceParams } from 'Shared';

export class a_view_model {
    constructor() {
        // Real command instances, not stub instances: generated commands keep their values in private fields,
        // which can only be set on an object the class actually constructed.
        this.replay = new Replay();
        this.replayResult = { onException: sinon.stub(), onValidationFailure: sinon.stub() };
        this.replayResult.onException.returns(this.replayResult);
        this.replayResult.onValidationFailure.returns(this.replayResult);
        this.replay.execute = sinon.stub().returns(this.replayResult);
        this.clearObserverQuarantine = new ClearObserverQuarantine();
        this.clearObserverQuarantine.execute = sinon.stub().returns({ onException: sinon.stub() });
        this.removeObserver = new RemoveObserver();
        this.removeObserverSucceedsWith(ObserverRemovalOutcome.removed);
        this.dialogs = sinon.createStubInstance(Dialogs);
        this.fastForward = new FastForward();
        this.fastForward.execute = sinon.stub().resolves({ isSuccess: true, response: new FastForwardResult(), onException: sinon.stub() });

        this.params = { eventStore: 'eventStore', namespace: 'namespace' };

        this.viewModel = new ObserversViewModel(this.replay, this.clearObserverQuarantine, this.removeObserver, this.dialogs, this.fastForward);
    }

    replay: Replay;
    replayResult: { onException: sinon.SinonStub; onValidationFailure: sinon.SinonStub };
    clearObserverQuarantine: ClearObserverQuarantine;
    removeObserver: RemoveObserver;
    fastForward: FastForward;
    dialogs: SinonStubbedInstance<Dialogs>;
    params: EventStoreAndNamespaceParams;
    viewModel: ObserversViewModel;

    /**
     * Make the remove command come back having run, reporting the given outcome.
     *
     * A refusal is a successful command that declined, not a failure, so it has to be modelled as a success carrying
     * the outcome - anything else would specify a path the kernel never takes.
     */
    removeObserverSucceedsWith(outcome: ObserverRemovalOutcome, blockingNamespace = '') {
        const response = new ObserverRemovalResult();
        response.outcome = outcome;
        response.blockingNamespace = blockingNamespace;

        this.removeObserver.execute = sinon.stub().resolves({
            isSuccess: true,
            response,
            onException: sinon.stub()
        });
    }
}
