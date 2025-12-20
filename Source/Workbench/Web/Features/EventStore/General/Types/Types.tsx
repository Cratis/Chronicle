// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import strings from 'Strings';
import { AllEventTypesParameters, AllEventTypesWithSchemas } from 'Api/EventTypes';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { FilterMatchMode } from 'primereact/api';
import { DataTableFilterMeta } from 'primereact/datatable';
import { TypesViewModel } from './TypesViewModel';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { useDialog } from '@cratis/arc.react.mvvm/dialogs';
import { AddEventType, AddEventTypeRequest, AddEventTypeResponse } from './AddEventType';
import { DataPage } from 'Components';
import { TypeDetails } from './TypeDetails';

const defaultFilters: DataTableFilterMeta = {
    tombstone: { value: null, matchMode: FilterMatchMode.IN },
};

const renderTombstone = () => {
    return 'no';
};

export const Types = withViewModel(TypesViewModel, () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [AddEventTypeDialog] = useDialog<AddEventTypeRequest, AddEventTypeResponse>(AddEventTypeRequest, AddEventType);

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

                {/* <DataPage.MenuItems>
                    <MenuItem id='create' label={strings.eventStore.general.types.actions.create} icon={faIcons.FaPlus} command={() => viewModel.addEventType()} />
                </DataPage.MenuItems> */}

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
            <AddEventTypeDialog/>
        </>
    );
});
