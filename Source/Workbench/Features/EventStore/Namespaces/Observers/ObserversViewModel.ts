// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { ObserverInformation } from 'Api/Concepts/Observation/ObserverInformation';
import { INamespaces } from 'State/Namespaces';
import * as Shared from 'Shared';
import { Namespace } from 'Api/Namespaces';

@injectable()
export class ObserversViewModel {

    constructor(
        namespaces: INamespaces,
        @inject('params') params: Shared.EventStoreAndNamespaceParams) {

        this.currentNamespace = { name: '', description: '' };

        namespaces.currentNamespace.subscribe(namespace => {
            this.currentNamespace = namespace;
        });
    }

    currentNamespace: Namespace;
    selectedObserver: ObserverInformation | undefined;
}
