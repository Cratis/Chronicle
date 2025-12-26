// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import strings from 'Strings';
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

const defaultFilters: DataTableFilterMeta = {
    tombstone: { value: null, matchMode: FilterMatchMode.IN },
};

const renderTombstone = () => {
    return 'no';
};

export const EventTypes = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [AddEventTypeWrapper, showAddEventType] = useDialog(AddEventTypeDialog);

    const queryArgs: AllEventTypesParameters = {
        eventStore: params.eventStore!
    };

    return (
        <>
            <DataPage
                title={strings.eventStore.general.types.title}
                query={AllEventTypesWithSchemas}
                queryArguments={queryArgs}
                dataKey='type.id'
                defaultFilters={defaultFilters}
                globalFilterFields={['tombstone']}
                emptyMessage={strings.eventStore.general.types.empty}
                detailsComponent={TypeDetails}>

                <DataPage.MenuItems>
                    <MenuItem
                        id='create'
                        label={strings.eventStore.general.types.actions.create}
                        icon={faIcons.FaPlus}
                        command={() => showAddEventType()} />
                </DataPage.MenuItems>

                <DataPage.Columns>

                    <Column field='type.id' header={strings.eventStore.general.types.columns.name} />
                    <Column
                        field='generation'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.types.columns.generation} />
                    <Column
                        field='tombstone'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.types.columns.tombstone}
                        body={renderTombstone} />
                </DataPage.Columns>
            </DataPage>
            <AddEventTypeWrapper />
        </>
    );
};
