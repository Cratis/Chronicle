// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState, type ChangeEvent } from 'react';
import { Column, DataTableCore } from '@cratis/components/DataTables';
import { Dropdown } from '@cratis/components/Dropdown';
import { Button } from '@cratis/components/Common';
import { ProgressSpinner } from '@cratis/components/Display';
import { InputText } from 'primereact/inputtext';
import * as faIcons from 'react-icons/fa6';
import { Json } from 'Features';
import { GraphProjectionPreview } from 'Features/ProjectionEditor';
import { ReadModelInstance } from 'Features/ReadModelExplorer';
import { ReadModelInstances } from 'Components/ReadModelInstances';
import { Tabs, TabPanel } from 'Components/Tabs';
import strings from 'Strings';
import { buildViews, graphViewDepths, rootFor, withRowIds, type GraphViewSettings } from './graphViews';

export interface GraphPreviewProps {
    /** The preview of the graph projection; nothing before the first preview. */
    preview: GraphProjectionPreview | null;
    /** Whether a preview is being made. */
    isPerforming: boolean;
    /** What the view tab reads. */
    settings: GraphViewSettings;
    /** Reads the view of the given settings from the graph of the preview. */
    onReadView: (settings: GraphViewSettings) => void;
}

const text = strings.eventStore.general.projections.graphPreview;

const describe = (properties: unknown): string => JSON.stringify(properties ?? {});

/**
 * Shows the graph a graph projection left behind in memory: its nodes, its edges and what a view reads from it.
 */
export const GraphPreview = ({ preview, isPerforming, settings, onReadView }: GraphPreviewProps) => {
    const [page, setPage] = useState(0);
    const [pageSize, setPageSize] = useState(50);
    const [selectedInstance, setSelectedInstance] = useState<Json | null>(null);
    const [draft, setDraft] = useState<GraphViewSettings>(settings);

    const graph = preview?.graph;
    const root = graph ? rootFor(graph, draft) : undefined;
    const built = useMemo(() => graph && root ? buildViews(graph, root, draft.depth) : undefined, [graph, root, draft.depth]);

    const nodes = useMemo(() => (preview?.nodes ?? []).map(node => ({
        id: `${node.label}:${node.key}`,
        label: node.label,
        key: node.key,
        properties: describe(node.properties)
    })), [preview?.nodes]);

    const edges = useMemo(() => (preview?.edges ?? []).map(edge => ({
        id: `${edge.type}:${edge.sourceKey}:${edge.targetKey}:${edge.identity ?? ''}`,
        type: edge.type,
        source: `${edge.sourceLabel} ${edge.sourceKey}`,
        target: `${edge.targetLabel} ${edge.targetKey}`,
        identity: edge.identity ?? '',
        properties: describe(edge.properties)
    })), [preview?.edges]);

    const rows = useMemo(() => withRowIds((preview?.rows ?? []) as unknown as Record<string, unknown>[], built?.keyProperty ?? '')
        .map(row => {
            const instance = new ReadModelInstance();
            instance.instance = row as unknown as Record<string, Record<string, unknown>>;
            return instance;
        }), [preview?.rows, built?.keyProperty]);

    if (!preview) {
        return <div className='p-4' data-testid='graph-preview-empty'>{isPerforming ? <ProgressSpinner aria-label={strings.general.loading} /> : text.notPreviewed}</div>;
    }

    const eventErrors = preview.eventErrors ?? [];
    const viewErrors = preview.viewErrors ?? [];

    return (
        <div className='p-4' style={{ height: '100%', display: 'flex', flexDirection: 'column', gap: '0.5rem' }} data-testid='graph-preview'>
            <div data-testid='graph-preview-summary' style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <faIcons.FaDiagramProject />
                <span>
                    {text.summary
                        .replace('{graph}', graph?.name ?? '')
                        .replace('{nodes}', String(nodes.length))
                        .replace('{edges}', String(edges.length))
                        .replace('{events}', String(preview.eventsProcessed))}
                </span>
                {isPerforming && <ProgressSpinner aria-label={strings.general.loading} style={{ width: '1.25rem', height: '1.25rem' }} />}
            </div>

            {eventErrors.length > 0 && (
                <div role='alert' data-testid='graph-preview-event-errors' style={{ color: 'var(--red-400, #f87171)' }}>
                    <strong>{text.eventErrors}</strong>
                    <ul style={{ margin: 0, paddingLeft: '1.25rem' }}>
                        {eventErrors.map(error => <li key={`${error.sequenceNumber}`}>#{error.sequenceNumber}: {error.message}</li>)}
                    </ul>
                </div>
            )}

            <Tabs panelContainerStyle={{ flex: 1, minHeight: 0, overflow: 'auto', padding: 0 }} style={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
                <TabPanel header={`${text.tabs.nodes} (${nodes.length})`}>
                    <DataTableCore<{ id: string }>
                        data={nodes}
                        dataKey='id'
                        emptyMessage={text.emptyNodes}
                        scrollable
                        scrollHeight='flex'>
                        <Column field='label' header={text.columns.label} sortable />
                        <Column field='key' header={text.columns.key} sortable />
                        <Column field='properties' header={text.columns.properties} />
                    </DataTableCore>
                </TabPanel>
                <TabPanel header={`${text.tabs.edges} (${edges.length})`}>
                    <DataTableCore<{ id: string }>
                        data={edges}
                        dataKey='id'
                        emptyMessage={text.emptyEdges}
                        scrollable
                        scrollHeight='flex'>
                        <Column field='type' header={text.columns.type} sortable />
                        <Column field='source' header={text.columns.source} sortable />
                        <Column field='target' header={text.columns.target} sortable />
                        <Column field='identity' header={text.columns.identity} />
                        <Column field='properties' header={text.columns.properties} />
                    </DataTableCore>
                </TabPanel>
                <TabPanel header={text.tabs.view}>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', height: '100%' }}>
                        <div data-testid='graph-view-controls' style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '0.5rem', padding: '0.5rem 0.5rem 0 0.5rem' }}>
                            <label>{text.view.root}</label>
                            <Dropdown<string>
                                optionLabel='label'
                                optionValue='value'
                                aria-label={text.view.root}
                                value={root ?? ''}
                                options={(graph?.nodeTypes ?? []).map(node => ({ label: node.label, value: node.label }))}
                                onChange={value => setDraft({ ...draft, root: value, property: undefined })} />
                            <label>{text.view.depth}</label>
                            <Dropdown<number>
                                optionLabel='label'
                                optionValue='value'
                                aria-label={text.view.depth}
                                value={draft.depth}
                                options={graphViewDepths.map(depth => ({ label: String(depth), value: depth }))}
                                onChange={value => setDraft({ ...draft, depth: value })} />
                            <label>{text.view.filterProperty}</label>
                            <Dropdown<string>
                                optionLabel='label'
                                optionValue='value'
                                aria-label={text.view.filterProperty}
                                value={draft.property ?? ''}
                                options={[{ label: text.view.anyProperty, value: '' }, ...(built?.properties ?? []).map(property => ({ label: property, value: property }))]}
                                onChange={value => setDraft({ ...draft, property: value || undefined })} />
                            <InputText
                                aria-label={text.view.filterValue}
                                placeholder={text.view.filterValue}
                                value={draft.value}
                                disabled={!draft.property}
                                onChange={(event: ChangeEvent<HTMLInputElement>) => setDraft({ ...draft, value: event.target.value })} />
                            <Button
                                label={text.view.read}
                                icon={<faIcons.FaPlay />}
                                disabled={!root || isPerforming}
                                onClick={() => {
                                    setPage(0);
                                    setSelectedInstance(null);
                                    onReadView({ ...draft, root });
                                }} />
                        </div>
                        {viewErrors.length > 0 && (
                            <div role='alert' data-testid='graph-view-errors' style={{ color: 'var(--red-400, #f87171)', padding: '0 0.5rem' }}>
                                <strong>{text.view.errors}</strong>
                                <ul style={{ margin: 0, paddingLeft: '1.25rem' }}>
                                    {viewErrors.map((error, index) => <li key={index}>{error.reason}{error.member ? ` (${error.view}.${error.member})` : ` (${error.view})`}: {error.message}</li>)}
                                </ul>
                            </div>
                        )}
                        <div style={{ flex: 1, minHeight: 0 }}>
                            <ReadModelInstances
                                instances={rows}
                                page={page}
                                pageSize={pageSize}
                                totalItems={rows.length}
                                isPerforming={isPerforming}
                                setPage={setPage}
                                setPageSize={setPageSize}
                                selectedInstance={selectedInstance}
                                setSelectedInstance={setSelectedInstance} />
                        </div>
                    </div>
                </TabPanel>
            </Tabs>
        </div>
    );
};
