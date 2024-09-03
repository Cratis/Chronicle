// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Page } from 'Components/Common/Page';
import { Column } from 'primereact/column';
import strings from 'Strings';
import { DataTableForQuery } from 'Components/DataTables';
import { AllEventTypes, AllEventTypesArguments } from 'Api/EventTypes';
import * as Shared from 'Shared';
import { useParams } from 'react-router-dom';
import { FilterMatchMode } from 'primereact/api';
import { DataTableFilterMeta } from 'primereact/datatable';
import { Menubar } from 'primereact/menubar';
import { FaPlus } from 'react-icons/fa';
import { MenuItem } from 'primereact/menuitem';
import { TypesViewModel } from './TypesViewModel';
import { withViewModel } from '@cratis/applications.react.mvvm';
import { useDialogRequest } from '@cratis/applications.react.mvvm/dialogs';
import { AddEventType, AddEventTypeRequest, AddEventTypeResponse } from './AddEventType';

const defaultFilters: DataTableFilterMeta = {
    tombstone: { value: null, matchMode: FilterMatchMode.IN },
};

export const Types = withViewModel(TypesViewModel, ({ viewModel }) => {
    const params = useParams<Shared.EventStoreAndNamespaceParams>();
    const [AddEventTypeDialogWrapper, addEventTypeDialogContext, addEventTypeDialogResolver]  = useDialogRequest<AddEventTypeRequest, AddEventTypeResponse>(AddEventTypeRequest);

    const queryArgs: AllEventTypesArguments = {
        eventStore: params.eventStore!
    };

    const menuItems: MenuItem[] = [
        {
            id: 'add',
            label: 'Add',
            icon: <FaPlus className={'mr-2'} />,
            command: () => viewModel.addEventType()
        }
    ];

    return (
        <Page title={strings.eventStore.general.types.title}>
            <div className="px-4 py-2">
                <Menubar aria-label='Actions' model={menuItems} />
            </div>

            <div className={'flex-1 overflow-hidden'}>
                <DataTableForQuery
                    query={AllEventTypes}
                    arguments={queryArgs}
                    dataKey='id'
                    defaultFilters={defaultFilters}
                    globalFilterFields={['tombstone']}
                    emptyMessage='No types found'>
                    <Column field='id' header={strings.eventStore.general.types.columns.name} sortable />
                    <Column field='generation' header={strings.eventStore.general.types.columns.generation} sortable />
                    <Column field='tombstone' header={strings.eventStore.general.types.columns.tombstone} sortable />
                </DataTableForQuery>
            </div>

            <AddEventTypeDialogWrapper>
                <AddEventType request={addEventTypeDialogContext.request} resolver={addEventTypeDialogResolver} />
            </AddEventTypeDialogWrapper>
        </Page>
    );
});
