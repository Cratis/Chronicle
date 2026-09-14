// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon, { SinonStubbedInstance } from 'sinon';
import { ObserversViewModel } from '../../../ObserversViewModel';
import { ClearObserverQuarantine, ReplayObserver as Replay } from 'Features/Observation';
import { Dialogs } from '@cratis/arc.react.mvvm/dialogs';
import { type EventStoreAndNamespaceParams } from 'Shared';

export class a_view_model {
    constructor() {
        this.replay = sinon.createStubInstance(Replay);
        this.replay.execute = sinon.stub().returns({ onException: sinon.stub() });
        this.clearObserverQuarantine = sinon.createStubInstance(ClearObserverQuarantine);
        this.clearObserverQuarantine.execute = sinon.stub().returns({ onException: sinon.stub() });
        this.dialogs = sinon.createStubInstance(Dialogs);

        this.params = { eventStore: 'eventStore', namespace: 'namespace' };

        this.viewModel = new ObserversViewModel(this.replay, this.clearObserverQuarantine, this.dialogs);
    }

    replay: Replay;
    clearObserverQuarantine: ClearObserverQuarantine;
    dialogs: SinonStubbedInstance<Dialogs>;
    params: EventStoreAndNamespaceParams;
    viewModel: ObserversViewModel;
}
