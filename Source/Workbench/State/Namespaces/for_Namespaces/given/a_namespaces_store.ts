// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon, { SinonStub } from 'sinon';
import { ILocalStorage } from '@cratis/arc.react.mvvm/browser';
import { IMessenger } from '@cratis/arc.react.mvvm/messaging';
import { ObserveNamespaces } from 'Features/Namespaces';
import { Namespaces } from '../../Namespaces';

type NamespacesPush = (result: { data: { name: string }[] }) => void;

export class a_namespaces_store {
    constructor() {
        this.storedNamespace = null;
        this.localStorage = {
            getItem: (key: string) => (key === 'namespace' ? this.storedNamespace : null),
            setItem: (key: string, value: string) => {
                if (key === 'namespace') this.storedNamespace = value;
            },
            removeItem: sinon.stub(),
            clear: sinon.stub()
        } as unknown as ILocalStorage;

        this.messenger = { publish: sinon.stub(), subscribe: sinon.stub() } as unknown as IMessenger;

        this.subscribe = sinon.stub().callsFake((callback: NamespacesPush) => {
            this.push = callback;
            return { unsubscribe: sinon.stub() };
        });
        this.query = { subscribe: this.subscribe } as unknown as ObserveNamespaces;

        this.store = new Namespaces(this.localStorage, this.messenger, this.query);
    }

    /** Delivers a namespaces result the way the observable query would. */
    pushNamespaces(...names: string[]) {
        this.push({ data: names.map(name => ({ name })) });
    }

    storedNamespace: string | null;
    localStorage: ILocalStorage;
    messenger: IMessenger;
    query: ObserveNamespaces;
    subscribe: SinonStub;
    push!: NamespacesPush;
    store: Namespaces;
}
