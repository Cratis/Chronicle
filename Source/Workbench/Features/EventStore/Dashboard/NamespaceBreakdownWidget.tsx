// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Link } from 'react-router-dom';
import { WidgetShell } from './WidgetShell';
import strings from 'Strings';

export interface INamespaceBreakdownWidget {

    /**
     * The namespaces of the event store.
     */
    namespaces: string[];

    /**
     * The base path to build the drill-down links from.
     */
    basePath: string;

    /**
     * Additional class names.
     */
    className?: string;
}

/**
 * Lists the namespaces of an event store, each linking to its own dashboard.
 */
export const NamespaceBreakdownWidget = ({ namespaces, basePath, className }: INamespaceBreakdownWidget) => (
    <WidgetShell
        title={strings.eventStore.dashboard.namespaces}
        subtitle={strings.eventStore.dashboard.namespacesSubtitle}
        className={className}>
        {namespaces.length === 0
            ? <span className='text-sm text-gray-400'>{strings.eventStore.dashboard.noNamespaces}</span>
            : (
                <div className='flex flex-col gap-1 overflow-auto'>
                    {namespaces.map(namespace => (
                        <Link
                            key={namespace}
                            to={`${basePath}/${namespace}/dashboard`}
                            className='flex justify-between gap-3 rounded px-2 py-1.5 text-sm hover:bg-[var(--cratis-surface-hover)]'>
                            <span className='truncate' title={namespace}>{namespace}</span>
                        </Link>
                    ))}
                </div>
            )}
    </WidgetShell>
);
