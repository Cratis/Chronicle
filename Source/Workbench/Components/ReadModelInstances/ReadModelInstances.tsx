// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useCallback } from 'react';
import { Column } from '@cratis/components/DataTables';
import { DataTableCore } from '@cratis/components/DataTables';
import strings from 'Strings';
import { Json } from 'Features';
import { ReadModelInstance } from 'Features/ReadModelExplorer';
import { ObjectNavigationalBar } from '@cratis/components/ObjectNavigationalBar';
import { ProgressSpinner } from '@cratis/components/Display';
import { InstanceCell } from './InstanceCell';
import { InstancePager } from './InstancePager';
import { useNestedBrowsing } from './useNestedBrowsing';
import { resolveSelection } from './nestedRows';
import './ReadModelInstances.css';

interface Props {
    instances: ReadModelInstance[];
    page: number;
    pageSize: number;
    totalItems: number;
    isPerforming: boolean;
    setPage: (p: number) => void;
    setPageSize: (s: number) => void;
    selectedInstance: Json | null;
    setSelectedInstance: (instance: Json | null) => void;
}

type Row = { [k: string]: Json };

export function ReadModelInstances({ instances, page, pageSize, totalItems, isPerforming, setPage, setPageSize, selectedInstance, setSelectedInstance }: Props) {
    const browsing = useNestedBrowsing(instances, { page, pageSize, totalItems });
    const { isNested, paging } = browsing;

    const columns = useMemo(() => browsing.columnKeys.map(key => (
        <Column
            key={key}
            field={key}
            header={key}
            sortable
            body={(rowData: Record<string, unknown>) => <InstanceCell value={rowData[key] as Json} onNavigate={() => browsing.navigateInto(key)} />}
        />
    )), [browsing.columnKeys, browsing.navigateInto]);

    // The table reports the row that was activated. A root row is stored as it is, so the table can match it
    // back to the row it came from, and clicking the row that is already selected clears the selection.
    // A nested row stands for the document it was found in, so that is what gets selected, and it always selects.
    const select = (clickedRow: Row | null) => {
        setSelectedInstance(resolveSelection(isNested, clickedRow, selectedInstance));
        browsing.returnToRoot();
    };

    const selectedRow = !isNested && selectedInstance && typeof selectedInstance === 'object' && !Array.isArray(selectedInstance) ? selectedInstance as Row : null;
    const changePage = useCallback((p: number) => isNested ? browsing.setNestedPage(p) : setPage(p), [isNested, browsing.setNestedPage, setPage]);
    const changePageSize = (size: number) => {
        if (isNested) {
            browsing.changeNestedPageSize(size);
        } else {
            setPageSize(size);
            setPage(0);
        }
    };

    return (
        <>
            <div className="p-4" style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                <ObjectNavigationalBar
                    navigationPath={browsing.navigationPath}
                    onNavigate={browsing.navigateToBreadcrumb}
                />

                <div
                    className="card"
                    style={{
                        display: 'flex',
                        flexDirection: 'column',
                        height: '100%',
                        border: '1px solid var(--surface-border)',
                        borderRadius: 'var(--border-radius)',
                        overflow: 'hidden'
                    }}>

                    <div style={{ flex: 1, minHeight: 0, overflowY: 'auto', overflowX: 'auto', position: 'relative' }}>
                        {isPerforming && (
                            <div className='read-model-instances__busy' data-testid='read-model-instances-busy'>
                                <ProgressSpinner aria-label={strings.general.loading} />
                            </div>
                        )}
                        <DataTableCore<Row>
                            data={browsing.rows}
                            dataKey={isNested ? '__nestedKey' : '_id'}
                            emptyMessage={isPerforming ? strings.general.loading : strings.eventStore.namespaces.readModels.empty}
                            selectionMode="single"
                            selection={selectedRow}
                            onSelectionChange={(event) => select(event.value)}
                            style={selectedInstance ? { minWidth: '100%', width: 'max-content' } : { minWidth: '100%' }}
                        >
                            {columns}
                        </DataTableCore>
                    </div>

                    {paging.totalItems > 0 && (
                        <InstancePager paging={paging} onPageChange={changePage} onPageSizeChange={changePageSize} />
                    )}
                </div>
            </div>

        </>
    );
}
