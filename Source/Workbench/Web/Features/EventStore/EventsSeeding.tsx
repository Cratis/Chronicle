// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataPage } from 'Components/DataPage/DataPage';
import { AllGlobalSeedEntries } from 'Api/Seeding/AllGlobalSeedEntries';
import { AllSeedEntriesForNamespace } from 'Api/Seeding/AllSeedEntriesForNamespace';
import { Column } from 'primereact/column';
import { SeedEntryDetails } from './Seeding/SeedEntryDetails';

export interface EventsSeedingComponentProps {
    eventStore: string;
    namespace?: string;
}

export const EventsSeeding = ({ eventStore, namespace }: EventsSeedingComponentProps) => {
    const title = namespace ? `Seed Data - ${namespace}` : 'Global Seed Data';

    if (namespace) {
        return (
            <DataPage
                title={title}
                query={AllSeedEntriesForNamespace}
                queryArguments={{ eventStore, namespace }}
                emptyMessage="No seed data found"
                dataKey="eventSourceId"
                detailsComponent={SeedEntryDetails}>
                <DataPage.Columns>
                    <Column field="eventSourceId" header="Event Source ID" sortable />
                    <Column field="eventTypeId" header="Event Type ID" sortable />
                </DataPage.Columns>
            </DataPage>
        );
    }

    return (
        <DataPage
            title={title}
            query={AllGlobalSeedEntries}
            queryArguments={{ eventStore }}
            emptyMessage="No seed data found"
            dataKey="eventSourceId"
            detailsComponent={SeedEntryDetails}>
            <DataPage.Columns>
                <Column field="eventSourceId" header="Event Source ID" sortable />
                <Column field="eventTypeId" header="Event Type ID" sortable />
            </DataPage.Columns>
        </DataPage>
    );
};
