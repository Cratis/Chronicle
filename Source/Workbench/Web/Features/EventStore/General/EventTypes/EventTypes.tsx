// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import strings from 'Strings';
import { useEffect } from 'react';
import { AllEventTypesParameters, AllEventTypesWithSchemas } from 'Api/EventTypes';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { FilterMatchMode } from 'primereact/api';
import { DataTableFilterMeta } from 'primereact/datatable';
import { useDialog } from '@cratis/arc.react/dialogs';
import { AddEventTypeDialog } from './Add/AddEventTypeDialog';
import { DataPage, MenuItem } from 'Components';
import { TypeDetails } from './TypeDetails';
import * as faIcons from 'react-icons/fa6';
import { EventTypeOwner, EventTypeRegistration, EventTypeSource } from 'Api/Events';

const defaultFilters: DataTableFilterMeta = {
    tombstone: { value: null, matchMode: FilterMatchMode.IN },
};

const renderTombstone = () => {
    return 'no';
};

const renderSource = (eventType: EventTypeRegistration) => {
    switch (eventType.source) {
        case EventTypeSource.code:
            return strings.eventStore.general.eventTypes.sources.code;
        case EventTypeSource.user:
            return strings.eventStore.general.eventTypes.sources.user;
    }
    return strings.eventStore.general.eventTypes.sources.unknown;
};

const renderOwner = (eventType: EventTypeRegistration) => {
    switch (eventType.owner) {
        case EventTypeOwner.client:
            return strings.eventStore.general.eventTypes.owners.client;
        case EventTypeOwner.server:
            return strings.eventStore.general.eventTypes.owners.server;
    }
    return strings.eventStore.general.eventTypes.owners.unknown;
};

export const EventTypes = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [AddEventTypeWrapper, showAddEventType] = useDialog(AddEventTypeDialog);

    const queryArgs: AllEventTypesParameters = {
        eventStore: params.eventStore!
    };

    useEffect(() => {
        const runDebug = () => {
            try {
                const selectors = [
                    'div.px-6.py-4',
                    'main.panel',
                    '.allotment',
                    '.allotment .allotment-pane',
                    'div.p-4',
                    'div.card',
                    '.p-datatable'
                ];

                console.group('EventTypes layout debug');
                selectors.forEach(sel => {
                    const el = document.querySelector(sel) as HTMLElement | null;
                    if (!el) {
                        console.log(`${sel}: not found`);
                        return;
                    }
                    const rect = el.getBoundingClientRect();
                    const cs = window.getComputedStyle(el);
                    console.log(sel, {
                        rect: { width: rect.width, height: rect.height, top: rect.top, left: rect.left },
                        display: cs.display,
                        position: cs.position,
                        height: cs.height,
                        minHeight: cs.minHeight,
                        maxHeight: cs.maxHeight,
                        flex: cs.flex,
                        overflow: cs.overflow,
                    });
                });
                console.groupEnd();
            } catch (err) {
                console.error('EventTypes layout debug failed', err);
            }
        };

        const t = setTimeout(runDebug, 300);
        return () => clearTimeout(t);
    }, []);

    return (
        <>
            <DataPage
                title={strings.eventStore.general.eventTypes.title}
                query={AllEventTypesWithSchemas}
                queryArguments={queryArgs}
                dataKey='type.id'
                defaultFilters={defaultFilters}
                globalFilterFields={['tombstone']}
                emptyMessage={strings.eventStore.general.eventTypes.empty}
                detailsComponent={TypeDetails}>

                <DataPage.MenuItems>
                    <MenuItem
                        id='create'
                        label={strings.eventStore.general.eventTypes.actions.create}
                        icon={faIcons.FaPlus}
                        command={() => showAddEventType()} />
                </DataPage.MenuItems>

                <DataPage.Columns>

                    <Column field='type.id' header={strings.eventStore.general.eventTypes.columns.name} />
                    <Column
                        field='owner'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.eventTypes.columns.owner}
                        body={renderOwner} />
                    <Column
                        field='source'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.eventTypes.columns.source}
                        body={renderSource} />
                    <Column
                        field='type.generation'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.eventTypes.columns.generation} />
                    <Column
                        field='tombstone'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.eventTypes.columns.tombstone}
                        body={renderTombstone} />
                </DataPage.Columns>
            </DataPage>
            <AddEventTypeWrapper />
        </>
    );
};
