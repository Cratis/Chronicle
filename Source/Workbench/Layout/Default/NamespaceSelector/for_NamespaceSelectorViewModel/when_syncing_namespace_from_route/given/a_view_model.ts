// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorSubject } from 'rxjs';
import sinon, { SinonStub } from 'sinon';
import { NamespaceSelectorViewModel, INamespaceSelectorProps } from '../../../NamespaceSelectorViewModel';
import { INamespaces } from 'State/Namespaces';

export class a_view_model {
    constructor() {
        this.setCurrentNamespace = sinon.stub();
        this.setRouteNamespace = sinon.stub();
        this.currentNamespaceSubject = new BehaviorSubject<string>('');
        this.namespaces = {
            currentNamespace: this.currentNamespaceSubject,
            setCurrentNamespace: this.setCurrentNamespace,
            namespaces: new BehaviorSubject<string[]>([]),
            setEventStore: sinon.stub(),
            setRouteNamespace: this.setRouteNamespace
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
    setRouteNamespace: SinonStub;
    currentNamespaceSubject: BehaviorSubject<string>;
    viewModel: NamespaceSelectorViewModel;
}
