// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataPage } from 'Components/DataPage/DataPage';
import { AllGlobalSeedEntries } from 'Api/Seeding/AllGlobalSeedEntries';
import { Column } from 'primereact/column';
import { useParams } from 'react-router-dom';

export const EventsSeeding = () => {
    const { eventStore } = useParams();

    return (
        <DataPage
            title="Seed Data"
            query={AllGlobalSeedEntries}
            queryArguments={{ eventStore }}
            emptyMessage="No seed data found"
            dataKey="eventSourceId">
            <DataPage.MenuItems>
                {/* TODO: Add menu items for adding seed data */}
            </DataPage.MenuItems>
            <DataPage.Columns>
                <Column field="eventSourceId" header="Event Source ID" sortable />
                <Column field="eventTypeId" header="Event Type ID" sortable />
                <Column field="isGlobal" header="Global" sortable body={(rowData) => rowData.isGlobal ? 'Yes' : 'No'} />
                <Column field="targetNamespace" header="Target Namespace" sortable />
            </DataPage.Columns>
        </DataPage>
    );
};
