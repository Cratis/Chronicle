// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GlobalSeedData } from 'Api/Seeding/GlobalSeedData';
import { NamespaceSeedData } from 'Api/Seeding/NamespaceSeedData';
import { SeedEntry } from 'Api/Seeding/SeedEntry';
import { Page } from 'Components/Common/Page';
import { Allotment } from 'allotment';
import { Column } from 'primereact/column';
import { DataTable, DataTableSelectionSingleChangeEvent } from 'primereact/datatable';
import { TabView, TabPanel } from 'primereact/tabview';
import { useMemo, useState } from 'react';
import { SeedEntryDetails } from './Seeding/SeedEntryDetails';

export interface EventsSeedingComponentProps {
    eventStore: string;
    namespace?: string;
}

interface FlatSeedEntry extends SeedEntry {
    _uniqueKey: string;
}

export const EventsSeeding = ({ eventStore, namespace }: EventsSeedingComponentProps) => {
    const title = namespace ? `Seed Data - ${namespace}` : 'Global Seed Data';
    const [selectedItem, setSelectedItem] = useState<FlatSeedEntry | undefined>(undefined);
    const [expandedRowsBySource, setExpandedRowsBySource] = useState<any>(null);
    const [expandedRowsByType, setExpandedRowsByType] = useState<any>(null);

    const [result] = namespace
        ? NamespaceSeedData.use({ eventStore, namespace })
        : GlobalSeedData.use({ eventStore });

    const byEventSource = useMemo(() => {
        const groups = result.data?.byEventSource ?? [];
        return groups.flatMap((group, gi) =>
            (group.entries ?? []).map((entry, ei) => ({
                ...entry,
                _uniqueKey: `src|${group.eventSourceId}|${entry.eventTypeId}|${gi}|${ei}`
            } as FlatSeedEntry))
        );
    }, [result.data]);

    const byEventType = useMemo(() => {
        const groups = result.data?.byEventType ?? [];
        return groups.flatMap((group, gi) =>
            (group.entries ?? []).map((entry, ei) => ({
                ...entry,
                _uniqueKey: `type|${group.eventTypeId}|${entry.eventSourceId}|${gi}|${ei}`
            } as FlatSeedEntry))
        );
    }, [result.data]);

    const onSelectionChange = (e: DataTableSelectionSingleChangeEvent<FlatSeedEntry[]>) => {
        setSelectedItem(e.value);
    };

    const contentPreview = (rowData: SeedEntry) => {
        const content = typeof rowData.content === 'string'
            ? rowData.content
            : JSON.stringify(rowData.content);
        return content.length > 50 ? content.substring(0, 50) + '...' : content;
    };

    const rowGroupHeaderTemplateBySource = (data: SeedEntry) => (
        <span style={{ fontWeight: 'bold' }}>Event Source: {data.eventSourceId}</span>
    );

    const rowGroupHeaderTemplateByType = (data: SeedEntry) => (
        <span style={{ fontWeight: 'bold' }}>Event Type: {data.eventTypeId}</span>
    );

    return (
        <Page title={title}>
            <div className="h-full" style={{ height: '100%' }}>
                <Allotment className="h-full" proportionalLayout={false}>
                    <Allotment.Pane className="flex-grow">
                        <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
                            <TabView style={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0 }}
                                panelContainerStyle={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, padding: 0 }}>
                                <TabPanel header="Per Event Source" contentStyle={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, padding: 0 }}>
                                    <div style={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, padding: '1rem', overflow: 'hidden' }}>
                                        <div style={{
                                            display: 'flex',
                                            flexDirection: 'column',
                                            flex: 1,
                                            minHeight: 0,
                                            border: '1px solid var(--surface-border)',
                                            borderRadius: 'var(--border-radius)',
                                            overflow: 'hidden'
                                        }}>
                                            <div style={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
                                                <DataTable
                                                    value={byEventSource}
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
                                                    scrollHeight="100%">
                                                    <Column field="eventTypeId" header="Event Type" sortable />
                                                    <Column field="content" header="Content Preview" body={contentPreview} />
                                                </DataTable>
                                            </div>
                                        </div>
                                    </div>
                                </TabPanel>
                                <TabPanel header="Per Event Type" contentStyle={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, padding: 0 }}>
                                    <div style={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, padding: '1rem', overflow: 'hidden' }}>
                                        <div style={{
                                            display: 'flex',
                                            flexDirection: 'column',
                                            flex: 1,
                                            minHeight: 0,
                                            border: '1px solid var(--surface-border)',
                                            borderRadius: 'var(--border-radius)',
                                            overflow: 'hidden'
                                        }}>
                                            <div style={{ flex: 1, minHeight: 0, overflow: 'auto' }}>
                                                <DataTable
                                                    value={byEventType}
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
                                                    scrollHeight="100%">
                                                    <Column field="eventTypeId" header="Event Type" sortable />
                                                    <Column field="eventSourceId" header="Event Source" sortable />
                                                    <Column field="content" header="Content Preview" body={contentPreview} />
                                                </DataTable>
                                            </div>
                                        </div>
                                    </div>
                                </TabPanel>
                            </TabView>
                        </div>
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
