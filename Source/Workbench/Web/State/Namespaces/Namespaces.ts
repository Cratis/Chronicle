// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessenger } from '@cratis/applications.react.mvvm/messaging';
import { inject, injectable } from 'tsyringe';
import { AllNamespaces } from 'Api/Namespaces';
import { ILocalStorage } from '@cratis/applications.react.mvvm/browser';
import { BehaviorSubject } from 'rxjs';
import { CurrentNamespaceChanged } from './CurrentNamespaceChanged';
import { INamespaces } from './INamespaces';
import { type EventStoreAndNamespaceParams } from 'Shared';

/**
 * Represents an implementation of {@link INamespaces}
 */
@injectable()
export class Namespaces implements INamespaces {
    private _currentNamespace: BehaviorSubject<string> = new BehaviorSubject('');
    private _namespaces: BehaviorSubject<string[]> = new BehaviorSubject<string[]>([]);

    constructor(
        private readonly _localStorage: ILocalStorage,
        private readonly _messenger: IMessenger,
        @inject('params') private readonly _params: EventStoreAndNamespaceParams,
        namespacesQuery: AllNamespaces) {

        namespacesQuery.subscribe(result => {
            this._namespaces.next(result.data);
            const namespace = this.getNamespaceFromName(this._params.namespace ?? this._localStorage.getItem('namespace'));
            if (namespace) {
                this.setCurrentNamespace(namespace);
            } else {
                this.setCurrentNamespace(this._namespaces.value[0]);
            }
        }, {
            eventStore: _params.eventStore
        });
    }

    /** @inheritdoc */
    get currentNamespace(): BehaviorSubject<string> {
        return this._currentNamespace;
    }

    /** @inheritdoc */
    setCurrentNamespace(namespace: string) {
        this._currentNamespace.next(namespace);
        this._localStorage.setItem('namespace', namespace);
        this._messenger.publish(new CurrentNamespaceChanged(namespace));
    }

    /** @inheritdoc */
    get namespaces(): BehaviorSubject<string[]> {
        return this._namespaces;
    }

    private getNamespaceFromName(name: string) {
        return this._namespaces.value.find(_ => _.toLowerCase() === name.toLowerCase());
    }
}
