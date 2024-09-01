// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { Namespace } from 'Api/Namespaces';
import { INamespaces } from 'State/Namespaces';

export interface INamespaceSelectorProps {
    onNamespaceSelected: (namespace: Namespace) => void;
}

@injectable()
export class NamespaceSelectorViewModel {
    constructor(
        private readonly _namespaces: INamespaces,
        @inject('props') private readonly _props: INamespaceSelectorProps) {

        _namespaces.currentNamespace.subscribe(namespace => {
            this.currentNamespace = namespace;
        });

        _namespaces.namespaces.subscribe(namespaces => {
            this.namespaces = namespaces;
        });
    }

    currentNamespace: Namespace = { name: '', description: '' };
    namespaces: Namespace[] = [];

    onNamespaceSelected(namespace: Namespace) {
        this.currentNamespace = namespace;
        this._props.onNamespaceSelected(namespace);
        this._namespaces.setCurrentNamespace(namespace);
    }

}
