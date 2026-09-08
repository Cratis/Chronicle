// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorSubject } from 'rxjs';
import sinon, { SinonStub } from 'sinon';
import { NamespaceSelectorViewModel, INamespaceSelectorProps } from '../../../NamespaceSelectorViewModel';
import { INamespaces } from 'State/Namespaces';

export class a_view_model {
    constructor() {
        this.setCurrentNamespace = sinon.stub();
        this.namespaces = {
            currentNamespace: new BehaviorSubject<string>(''),
            setCurrentNamespace: this.setCurrentNamespace,
            namespaces: new BehaviorSubject<string[]>([]),
            setEventStore: sinon.stub()
        };
        this.props = {
            onNamespaceSelected: sinon.stub()
        };

        this.viewModel = new NamespaceSelectorViewModel(this.namespaces, this.props);
        this.viewModel.currentNamespace = 'current-namespace';
    }

    namespaces: INamespaces;
    props: INamespaceSelectorProps;
    setCurrentNamespace: SinonStub;
    viewModel: NamespaceSelectorViewModel;
}
