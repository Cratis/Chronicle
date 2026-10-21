// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useCallback, useContext, useEffect, useMemo, useState, ReactNode } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { ObserveNamespaces } from 'Features/Namespaces';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { replaceEventStoreNamespaceInPath } from '../replaceEventStoreNamespaceInPath';
import { readRememberedNamespace, writeRememberedNamespace } from '../rememberedNamespace';
import { resolveNamespace } from '../resolveNamespace';

export interface IWorkbenchContext {
    pageTitle: string;
    setPageTitle: (title: string) => void;

    /** The event store the Workbench is showing. */
    eventStore: string;

    /** The namespace the Workbench is working in. It follows the address, so it survives a refresh. */
    namespace: string;

    /** The namespaces of the current event store. */
    namespaces: string[];

    /** Moves the Workbench, and the page it is on, to another namespace. */
    selectNamespace: (namespace: string) => void;
}

const defaultWorkbenchContext: IWorkbenchContext = {
    pageTitle: '',
    setPageTitle: () => null,
    eventStore: '',
    namespace: '',
    namespaces: [],
    selectNamespace: () => null,
};

export const WorkbenchContext = createContext<IWorkbenchContext>(defaultWorkbenchContext);

export const useWorkbenchContext = () => useContext(WorkbenchContext);

/**
 * Holds what the Workbench is looking at: the event store and the namespace.
 *
 * The address is the only source of truth. Nothing here decides a namespace by itself and navigates to
 * it, so a refresh, a shared link or the back button cannot be overridden by state kept somewhere else.
 * Choosing a namespace changes the address, and everything else follows from that.
 * @param props The children to provide the context to.
 * @returns The provider.
 */
export const WorkbenchProvider = (props: { children: ReactNode }) => {
    const { eventStore = '', namespace: routeNamespace } = useParams<EventStoreAndNamespaceParams>();
    const location = useLocation();
    const navigate = useNavigate();
    const [pageTitle, setPageTitle] = useState<string>('');
    const [namespacesResult] = ObserveNamespaces.use({ eventStore });
    const [chosen, setChosen] = useState<string | undefined>(() => readRememberedNamespace(eventStore));

    const namespaces = useMemo(() => namespacesResult.data.map(_ => _.name), [namespacesResult.data]);

    useEffect(() => {
        setChosen(readRememberedNamespace(eventStore));
    }, [eventStore]);

    useEffect(() => {
        if (routeNamespace) {
            setChosen(routeNamespace);
            writeRememberedNamespace(eventStore, routeNamespace);
        }
    }, [eventStore, routeNamespace]);

    const namespace = resolveNamespace(routeNamespace, chosen, namespaces);

    const selectNamespace = useCallback((next: string) => {
        setChosen(next);
        writeRememberedNamespace(eventStore, next);

        const path = replaceEventStoreNamespaceInPath(location.pathname, eventStore, routeNamespace, next);
        if (path !== location.pathname) {
            navigate(path);
        }
    }, [eventStore, routeNamespace, location.pathname, navigate]);

    const value = useMemo(() => ({
        pageTitle,
        setPageTitle,
        eventStore,
        namespace,
        namespaces,
        selectNamespace,
    }), [pageTitle, eventStore, namespace, namespaces, selectNamespace]);

    return (
        <WorkbenchContext.Provider value={value}>
            {props.children}
        </WorkbenchContext.Provider>
    );
};
