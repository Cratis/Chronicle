// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { AllNamespaces, Namespace } from 'Api/Namespaces';
import { ILocalStorage, INavigation } from '@cratis/applications.react.mvvm/browser';

export interface INamespaceSelectorProps {
    onNamespaceSelected: (namespace: Namespace) => void;
}

export interface IParams {
    namespace: string;
}

@injectable()
export class NamespaceSelectorViewModel {
    constructor(
        private readonly _namespaces: AllNamespaces,
        private readonly _localStorage: ILocalStorage,
        private readonly _navigation: INavigation,
        @inject('props') private readonly _props: INamespaceSelectorProps,
        @inject('params') private readonly _params: any) {

        this._navigation.onUrlChanged((url, previousUrl) => {
            console.log('Url changed', url, previousUrl);
        });

        this._namespaces.subscribe(result => {
            this.namespaces = result.data;

            const namespace = this.getNamespaceFromName(this._params.namespace ?? this._localStorage.getItem('namespace'));
            if (namespace) {
                this.currentNamespace = namespace;
            } else {
                this.currentNamespace = this.namespaces[0];
            }
        }, {
            eventStore: _params.eventStoreId
        });
    }

    currentNamespace: Namespace = { name: '', description: '' };
    namespaces: Namespace[] = [];

    onNamespaceSelected(namespace: Namespace) {
        this.currentNamespace = namespace;
        this._localStorage.setItem('namespace', namespace.name);
        this._props.onNamespaceSelected(namespace);
    }

    private getNamespaceFromName(name: string) {
        return this.namespaces.find(_ => _.name === name);
    }
}
