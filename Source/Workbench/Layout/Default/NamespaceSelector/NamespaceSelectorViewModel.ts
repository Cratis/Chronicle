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

        // Display only. Navigating from here would mean any code path that touches the store steers
        // the browser - which is how a routine namespaces push used to drag the user back to
        // whichever namespace the application started on. Navigation belongs to the click handler.
        _namespaces.currentNamespace.subscribe(namespace => {
            if (namespace) {
                this.currentNamespace = namespace;
            }
        });

        _namespaces.namespaces.subscribe(namespaces => {
            this.namespaces = namespaces;
        });
    }

    currentNamespace: string = '';
    namespaces: string[] = [];

    setEventStore(eventStore: string) {
        this._namespaces.setEventStore(eventStore);
    }

    onNamespaceSelected(namespace: string) {
        this.currentNamespace = namespace;
        this._props.onNamespaceSelected(namespace);
        this._namespaces.setCurrentNamespace(namespace);
    }

    /**
     * Keeps the global namespace store in sync with the route whenever the namespace
     * changes through a means other than this selector (browser back/forward, a deep
     * link, or a directly-typed URL).
     * @param namespace - The namespace currently in the route.
     */
    syncNamespaceFromRoute(namespace: string) {
        this._namespaces.setRouteNamespace(namespace || undefined);
        if (namespace && namespace !== this.currentNamespace) {
            this._namespaces.setCurrentNamespace(namespace);
        }
    }
}
