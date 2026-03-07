// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { INamespaces } from 'State/Namespaces';

export interface INamespaceSelectorProps {
    onNamespaceSelected: (namespace: string) => void;
}

@injectable()
export class NamespaceSelectorViewModel {
    constructor(
        private readonly _namespaces: INamespaces,
        @inject('props') private readonly _props: INamespaceSelectorProps) {

        _namespaces.currentNamespace.subscribe(namespace => {
            if (namespace) {
                this.currentNamespace = namespace;
                this._props.onNamespaceSelected(namespace);
            }
        });

        _namespaces.namespaces.subscribe(namespaces => {
            this.namespaces = namespaces;
        });
    }

    currentNamespace: string = '';
    namespaces: string[] = [];

    onNamespaceSelected(namespace: string) {
        this.currentNamespace = namespace;
        this._props.onNamespaceSelected(namespace);
        this._namespaces.setCurrentNamespace(namespace);
    }
}
