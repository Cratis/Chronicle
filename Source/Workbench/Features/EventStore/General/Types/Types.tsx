// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Page } from 'Components/Common/Page';
import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import strings from 'Strings';

export const Types = () => {

    return (
        <Page title={strings.eventStore.general.types.title} mainClassName={'overflow-hidden flex flex-col h-full'}>
            <div className={'flex-1 overflow-hidden'}>
                <DataTable
                        value={[]}
                        rows={100}
                        paginator
                        alwaysShowPaginator={false}
                        scrollable
                        scrollHeight={'flex'}
                        selectionMode='single'
                        onSelectionChange={(e) => {}}
                        dataKey='observerId'
                        filterDisplay='menu'
                        emptyMessage='No types found'
                    >
                        <Column field='name' header={strings.eventStore.general.types.columns.name} sortable />
                        <Column field='generation' header={strings.eventStore.general.types.columns.generation} sortable />
                        <Column field='tombstone' header={strings.eventStore.general.types.columns.tombstone} sortable />
                </DataTable>
            </div>
        </Page>
    );
};
