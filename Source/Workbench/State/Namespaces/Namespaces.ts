// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessenger } from '@cratis/applications.react.mvvm/messaging';
import { inject, injectable } from 'tsyringe';
import { Namespace, AllNamespaces } from 'Api/Namespaces';
import { ILocalStorage } from '@cratis/applications.react.mvvm/browser';
import { BehaviorSubject } from 'rxjs';
import { CurrentNamespaceChanged } from './CurrentNamespaceChanged';
import { INamespaces } from './INamespaces';

export interface IParams {
    eventStoreId: string;
    namespace: string;
}

/**
 * Represents an implementation of {@link INamespaces}
 */
@injectable()
export class Namespaces implements INamespaces {
    private _currentNamespace: BehaviorSubject<Namespace> = new BehaviorSubject({ name: '', description: '' });
    private _namespaces: BehaviorSubject<Namespace[]> = new BehaviorSubject<Namespace[]>([]);

    constructor(
        readonly namespacesQuery: AllNamespaces,
        private readonly _localStorage: ILocalStorage,
        private readonly _messenger: IMessenger,
        @inject('params') private readonly _params: IParams) {

        namespacesQuery.subscribe(result => {
            this._namespaces.next(result.data);
            const namespace = this.getNamespaceFromName(this._params.namespace ?? this._localStorage.getItem('namespace'));
            if (namespace) {
                this.setCurrentNamespace(namespace);
            } else {
                this.setCurrentNamespace(this._namespaces.value[0]);
            }
        }, {
            eventStore: _params.eventStoreId
        });
    }

    /** @inheritdoc */
    get currentNamespace(): BehaviorSubject<Namespace> {
        return this._currentNamespace;
    }

    /** @inheritdoc */
    setCurrentNamespace(namespace: Namespace) {
        this._currentNamespace.next(namespace);
        this._localStorage.setItem('namespace', namespace.name);
        this._messenger.publish(new CurrentNamespaceChanged(namespace));
    }

    /** @inheritdoc */
    get namespaces(): BehaviorSubject<Namespace[]> {
        return this._namespaces;
    }

    private getNamespaceFromName(name: string) {
        return this._namespaces.value.find(_ => _.name.toLowerCase() === name.toLowerCase());
    }
}
