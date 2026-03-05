// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorSubject } from 'rxjs';
import sinon, { SinonStubbedInstance } from 'sinon';
import { ObserversViewModel } from '../../../ObserversViewModel';
import { INamespaces } from 'State/Namespaces';
import { Replay } from 'Api/Observation';
import { Dialogs } from '@cratis/arc.react.mvvm/dialogs';
import { type EventStoreAndNamespaceParams } from 'Shared';

export class a_view_model {
    constructor() {
        this.namespaces = {
            currentNamespace: new BehaviorSubject<string>(''),
            setCurrentNamespace: sinon.stub(),
            namespaces: new BehaviorSubject<string[]>([])

        };
        this.replay = sinon.createStubInstance(Replay);
        this.replay.execute = sinon.stub().returns({ onException: sinon.stub() });
        this.dialogs = sinon.createStubInstance(Dialogs);

        this.params = { eventStore: 'eventStore', namespace: 'namespace' };

        this.viewModel = new ObserversViewModel(this.namespaces, this.replay, this.dialogs, this.params);
    }

    namespaces: INamespaces;
    replay: Replay;
    dialogs: SinonStubbedInstance<Dialogs>;
    params: EventStoreAndNamespaceParams;
    viewModel: ObserversViewModel;
}

