// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllGlobalSeedEntries } from 'Api/Seeding/AllGlobalSeedEntries';
import { AllSeedEntriesForNamespace } from 'Api/Seeding/AllSeedEntriesForNamespace';
import { SeedEntry } from 'Api/Seeding/SeedEntry';
import { Page } from 'Components/Common/Page';
import { Allotment } from 'allotment';
import { Column } from 'primereact/column';
import { DataTable, DataTableSelectionSingleChangeEvent } from 'primereact/datatable';
import { TabView, TabPanel } from 'primereact/tabview';
import { useState } from 'react';
import { SeedEntryDetails } from './Seeding/SeedEntryDetails';

export interface EventsSeedingComponentProps {
    eventStore: string;
    namespace?: string;
}

export const EventsSeeding = ({ eventStore, namespace }: EventsSeedingComponentProps) => {
    const title = namespace ? `Seed Data - ${namespace}` : 'Global Seed Data';
    const [selectedItem, setSelectedItem] = useState<SeedEntry | undefined>(undefined);
    const [expandedRowsBySource, setExpandedRowsBySource] = useState<any>(null);
    const [expandedRowsByType, setExpandedRowsByType] = useState<any>(null);

    const queryHook = namespace ? AllSeedEntriesForNamespace : AllGlobalSeedEntries;
    const queryArguments = namespace ? { eventStore, namespace } : { eventStore };
    const [result] = queryHook.use(queryArguments as any);

    // Add unique key to each entry for proper selection
    const dataWithKeys = (result.data || []).map((entry, index) => ({
        ...entry,
        _uniqueKey: `${entry.eventSourceId}|${entry.eventTypeId}|${index}`
    }));

    const onSelectionChange = (e: DataTableSelectionSingleChangeEvent<SeedEntry[]>) => {
        setSelectedItem(e.value);
    };

    const rowGroupHeaderTemplateBySource = (data: SeedEntry) => {
        return (
            <span style={{ fontWeight: 'bold' }}>
                Event Source: {data.eventSourceId}
            </span>
        );
    };

    const rowGroupHeaderTemplateByType = (data: SeedEntry) => {
        return (
            <span style={{ fontWeight: 'bold' }}>
                Event Type: {data.eventTypeId}
            </span>
        );
    };

    return (
        <Page title={title}>
            <div className="h-full" style={{ height: '100%' }}>
                <Allotment className="h-full" proportionalLayout={false}>
                    <Allotment.Pane className="flex-grow">
                        <TabView style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                            <TabPanel header="Per Event Source">
                                <div style={{ padding: '1rem', height: '100%', display: 'flex', flexDirection: 'column' }}>
                                    <div style={{
                                        display: 'flex',
                                        flexDirection: 'column',
                                        height: '100%',
                                        border: '1px solid var(--surface-border)',
                                        borderRadius: 'var(--border-radius)',
                                        overflow: 'hidden'
                                    }}>
                                        <DataTable
                                            value={dataWithKeys}
                                            rowGroupMode="subheader"
                                            groupRowsBy="eventSourceId"
                                            sortMode="single"
                                            sortField="eventSourceId"
                                            sortOrder={1}
                                            expandableRowGroups
                                            expandedRows={expandedRowsBySource}
                                            onRowToggle={(e) => setExpandedRowsBySource(e.data)}
                                            rowGroupHeaderTemplate={rowGroupHeaderTemplateBySource}
                                            selectionMode="single"
                                            selection={selectedItem}
                                            onSelectionChange={onSelectionChange}
                                            dataKey="_uniqueKey"
                                            emptyMessage="No seed data found"
                                            scrollable
                                            scrollHeight="flex"
                                            style={{ height: '100%' }}>
                                            <Column field="eventTypeId" header="Event Type" sortable />
                                            <Column field="content" header="Content Preview" body={(rowData: SeedEntry) => {
                                                const content = typeof rowData.content === 'string'
                                                    ? rowData.content
                                                    : JSON.stringify(rowData.content);
                                                return content.length > 50 ? content.substring(0, 50) + '...' : content;
                                            }} />
                                        </DataTable>
                                    </div>
                                </div>
                            </TabPanel>
                            <TabPanel header="Per Event Type">
                                <div style={{ padding: '1rem', height: '100%', display: 'flex', flexDirection: 'column' }}>
                                    <div style={{
                                        display: 'flex',
                                        flexDirection: 'column',
                                        height: '100%',
                                        border: '1px solid var(--surface-border)',
                                        borderRadius: 'var(--border-radius)',
                                        overflow: 'hidden'
                                    }}>
                                        <DataTable
                                            value={dataWithKeys}
                                            rowGroupMode="subheader"
                                            groupRowsBy="eventTypeId"
                                            sortMode="single"
                                            sortField="eventTypeId"
                                            sortOrder={1}
                                            expandableRowGroups
                                            expandedRows={expandedRowsByType}
                                            onRowToggle={(e) => setExpandedRowsByType(e.data)}
                                            rowGroupHeaderTemplate={rowGroupHeaderTemplateByType}
                                            selectionMode="single"
                                            selection={selectedItem}
                                            onSelectionChange={onSelectionChange}
                                            dataKey="_uniqueKey"
                                            emptyMessage="No seed data found"
                                            scrollable
                                            scrollHeight="flex"
                                            style={{ height: '100%' }}>
                                            <Column field="eventTypeId" header="Event Type" sortable />
                                            <Column field="eventSourceId" header="Event Source" sortable />
                                            <Column field="content" header="Content Preview" body={(rowData: SeedEntry) => {
                                                const content = typeof rowData.content === 'string'
                                                    ? rowData.content
                                                    : JSON.stringify(rowData.content);
                                                return content.length > 50 ? content.substring(0, 50) + '...' : content;
                                            }} />
                                        </DataTable>
                                    </div>
                                </div>
                            </TabPanel>
                        </TabView>
                    </Allotment.Pane>
                    {selectedItem && (
                        <Allotment.Pane preferredSize="450px">
                            <SeedEntryDetails item={selectedItem} />
                        </Allotment.Pane>
                    )}
                </Allotment>
            </div>
        </Page>
    );
};
