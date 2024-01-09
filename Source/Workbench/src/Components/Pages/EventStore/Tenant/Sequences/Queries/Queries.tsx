// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChangeEvent, useCallback, useRef, useState } from 'react';
import { QueryHeader, QueryType } from './components/QueryHeader';
import { QueryMenuActions } from './components/QueryMenuActions';
import { QuerySidebar } from './components/QuerySidebar';
import { TabView, TabPanel } from 'primereact/tabview';
import { OverlayPanel } from 'primereact/overlaypanel';
import { QueriesViewModel } from './QueriesViewModel';
import { QueryTable } from './components/QueryTable';
import { withViewModel } from 'MVVM/withViewModel';
import { Bookmark } from './Bookmark/Bookmark';
import { Button } from 'primereact/button';
import css from './Queries.module.css';

const initialQueries = [
    { title: 'Query 1', id: '1' },
    { title: 'Query 2', id: '2' },
];

export const Queries = withViewModel(QueriesViewModel, () => {
    const overlayPanelRef = useRef<OverlayPanel>(null);
    const [isSidebarOpen, setSidebarOpen] = useState(false);
    const [currentQuery, setCurrentQuery] = useState<number>(1);
    const [queries, setQueries] = useState<QueryType[]>(initialQueries);

    const addQuery = useCallback(() => {
        setQueries((prevQueries) => [
            ...prevQueries,
            {
                title: `Query ${prevQueries.length + 1}`,
                id: `${prevQueries.length + 1}`,
                isEditing: false,
            },
        ]);
    }, []);

    const updateQuery = (idx: number, update: Partial<QueryType>) =>
        setQueries((prevQueries) =>
            prevQueries.map((query, index) =>
                index === idx ? { ...query, ...update } : query
            )
        );

    const onQueryChange = useCallback(
        (evt: ChangeEvent<HTMLInputElement>, idx: number) => {
            updateQuery(idx, { title: evt.target.value });
        },
        []
    );

    const toggleEdit = useCallback(
        (idx: number) => {
            updateQuery(idx, { isEditing: queries[idx].isEditing ? false : true });
        },
        [queries]
    );

    const renderQueryTabPanel = (query: QueryType, idx: number) => {
        return (
            <TabPanel
                key={query.id}
                closable={idx !== 0}
                className={css.activeTab}
                header={
                    <QueryHeader
                        idx={idx}
                        query={query}
                        onToggleEdit={toggleEdit}
                        onQueryChange={onQueryChange}
                    />
                }
            >
                <div className={css.panelContainer}>
                    <div>
                        <QuerySidebar
                            isSidebarOpen={isSidebarOpen}
                            toggleSidebar={() => setSidebarOpen(!isSidebarOpen)}
                        >
                            <div>Select Event</div>
                            <div className={css.sidebarActions}>
                                <Button text>Logs</Button>
                                <Button text>Outbox</Button>
                                <Button text>People</Button>
                            </div>
                        </QuerySidebar>
                    </div>
                    <div className={css.panelBody}>
                        <QueryMenuActions />
                        <QueryTable queryNumber={query.id} />
                    </div>
                </div>
            </TabPanel>
        );
    };

    return (
        <div className={css.container}>
            <TabView
                pt={{
                    panelContainer: () => ({
                        className: css.tabPanel,
                    }),
                }}
                scrollable
                className={css.tabView}
                activeIndex={currentQuery}
                onTabChange={(e) => setCurrentQuery(e.index)}
            >
                <TabPanel
                    headerTemplate={
                        <div className={css.buttonContainer}>
                            <Button
                                icon='pi pi-book'
                                onClick={(e) => overlayPanelRef.current?.toggle(e)}
                            />
                            <OverlayPanel ref={overlayPanelRef}>
                                <Bookmark />
                            </OverlayPanel>
                        </div>
                    }
                />

                {queries.map(renderQueryTabPanel)}

                <TabPanel
                    headerTemplate={<Button text icon='pi pi-plus' onClick={addQuery} />}
                />
            </TabView>
        </div>
    );
});
