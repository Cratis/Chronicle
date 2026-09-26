// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessenger } from '@cratis/arc.react.mvvm/messaging';
import { injectable } from 'tsyringe';
import { ObserveNamespaces } from 'Features/Namespaces';
import { ILocalStorage } from '@cratis/arc.react.mvvm/browser';
import { BehaviorSubject } from 'rxjs';
import type { ObservableQuerySubscription } from '@cratis/arc/queries';
import { CurrentNamespaceChanged } from './CurrentNamespaceChanged';
import { INamespaces } from './INamespaces';

/**
 * Represents an implementation of {@link INamespaces}
 */
@injectable()
export class Namespaces implements INamespaces {
    private _currentNamespace: BehaviorSubject<string> = new BehaviorSubject('');
    private _namespaces: BehaviorSubject<string[]> = new BehaviorSubject<string[]>([]);
    private _subscription: ObservableQuerySubscription<string[]> | null = null;
    private _lastEventStore: string | undefined = undefined;
    private _eventStore: string | undefined = undefined;
    private _routeNamespace: string | undefined = undefined;

    constructor(
        private readonly _localStorage: ILocalStorage,
        private readonly _messenger: IMessenger,
        private readonly _namespacesQuery: ObserveNamespaces) {
    }

    /** @inheritdoc */
    setEventStore(eventStore: string) {
        if (eventStore) {
            this._eventStore = eventStore;
        }
        this.ensureSubscription();
    }

    /** @inheritdoc */
    setRouteNamespace(namespace: string | undefined) {
        this._routeNamespace = namespace;
    }

    private ensureSubscription() {
        // Only subscribe if we have an eventStore and it's different from last time
        if (!this._eventStore) {
            return;
        }

        if (this._lastEventStore === this._eventStore) {
            return; // Already subscribed for this event store
        }

        // Unsubscribe from previous subscription
        if (this._subscription) {
            this._subscription.unsubscribe();
            this._subscription = null;
        }

        this._lastEventStore = this._eventStore;

        this._subscription = this._namespacesQuery.subscribe(result => {
            this._namespaces.next(result.data.map(namespace => namespace.name));
            this.reconcileCurrentNamespace();
        }, {
            eventStore: this._eventStore
        });
    }

    /**
     * Settles on a current namespace after the known set has changed.
     *
     * This runs on every push of the namespaces query, which is a routine event and on its own no
     * reason to move the user somewhere else - so a current namespace that still exists is left
     * exactly where it is. Only when there is none, or the one held no longer exists, is a new one
     * chosen: the route's, then the last one the user picked, then whatever is first.
     */
    private reconcileCurrentNamespace() {
        const current = this.getNamespaceFromName(this._currentNamespace.value);
        if (current) {
            return;
        }

        const resolved =
            this.getNamespaceFromName(this._routeNamespace) ??
            this.getNamespaceFromName(this._localStorage.getItem('namespace')) ??
            this._namespaces.value[0];

        if (resolved) {
            this.setCurrentNamespace(resolved);
        }
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

    private getNamespaceFromName(name: string | undefined | null) {
        if (!name) {
            return undefined;
        }
        return this._namespaces.value.find(_ => _.toLowerCase() === name.toLowerCase());
    }
}
