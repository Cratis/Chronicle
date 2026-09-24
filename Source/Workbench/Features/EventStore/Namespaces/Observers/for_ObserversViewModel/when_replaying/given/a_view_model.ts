// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon, { SinonStubbedInstance } from 'sinon';
import { ObserversViewModel } from '../../../ObserversViewModel';
import { ClearObserverQuarantine, RemoveObserver, ReplayObserver as Replay } from 'Features/Observation';
import { ObserverRemovalOutcome, ObserverRemovalResult } from 'Features/Concepts/Observation';
import { Dialogs } from '@cratis/arc.react.mvvm/dialogs';
import { type EventStoreAndNamespaceParams } from 'Shared';

export class a_view_model {
    constructor() {
        this.replay = sinon.createStubInstance(Replay);
        this.replay.execute = sinon.stub().returns({ onException: sinon.stub() });
        this.clearObserverQuarantine = sinon.createStubInstance(ClearObserverQuarantine);
        this.clearObserverQuarantine.execute = sinon.stub().returns({ onException: sinon.stub() });
        this.removeObserver = sinon.createStubInstance(RemoveObserver);
        this.removeObserverSucceedsWith(ObserverRemovalOutcome.removed);
        this.dialogs = sinon.createStubInstance(Dialogs);

        this.params = { eventStore: 'eventStore', namespace: 'namespace' };

        this.viewModel = new ObserversViewModel(this.replay, this.clearObserverQuarantine, this.removeObserver, this.dialogs);
    }

    replay: Replay;
    clearObserverQuarantine: ClearObserverQuarantine;
    removeObserver: RemoveObserver;
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
