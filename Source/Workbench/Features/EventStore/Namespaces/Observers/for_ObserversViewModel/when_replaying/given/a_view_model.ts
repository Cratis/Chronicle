// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorSubject } from 'rxjs';
import sinon, { SinonStubbedInstance } from 'sinon';
import { ObserversViewModel } from '../../../ObserversViewModel';
import { INamespaces } from 'State/Namespaces';
import { Namespace } from 'Api/Namespaces';
import { Replay } from 'Api/Observation';
import { Dialogs } from '@cratis/applications.react.mvvm/dialogs';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { Guid } from '@cratis/fundamentals';

export class a_view_model {
    constructor() {
        this.namespaces = {
            currentNamespace: new BehaviorSubject<Namespace>({ id: Guid.empty, name: '', description: '' }),
            setCurrentNamespace: sinon.stub(),
            namespaces: new BehaviorSubject<Namespace[]>([])

        };
        this.replay = sinon.createStubInstance(Replay);
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

