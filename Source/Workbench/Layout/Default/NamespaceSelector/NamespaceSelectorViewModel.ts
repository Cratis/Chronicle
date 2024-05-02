// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { AllNamespaces, Namespace } from 'API/Namespaces';

@injectable()
export class NamespaceSelectorViewModel {
    constructor(private readonly _namespaces: AllNamespaces) {
        this._namespaces.subscribe(result => {
            this.namespaces = result.data;
        });

        this.currentNamespace = this.namespaces[0];
    }

    currentNamespace: Namespace = { name: '', description: '' };
    namespaces: Namespace[] = [];

    getNamespaceFromName(name: string) {
        return this.namespaces.find(_ => _.name === name);
    }
}
