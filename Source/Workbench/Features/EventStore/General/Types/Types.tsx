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

const defaultFilters: DataTableFilterMeta = {
    tombstone: { value: null, matchMode: FilterMatchMode.IN },
};

export const Types = () => {
    const params = useParams<Shared.EventStoreAndNamespaceParams>();

    const queryArgs: AllEventTypesArguments = {
        eventStore: params.eventStore
    };

    return (
        <Page title={strings.eventStore.general.types.title}>
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
        </Page>
    );
};
