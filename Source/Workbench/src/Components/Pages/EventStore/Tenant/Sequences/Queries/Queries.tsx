// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChangeEvent, useCallback, useRef, useState } from 'react';
import { TabView, TabPanel } from 'primereact/tabview';
import { QueryHeader, QueryType } from './QueryHeader';
import { OverlayPanel } from 'primereact/overlaypanel';
import { QueryMenuActions } from './QueryMenuActions';
import { QueriesViewModel } from './QueriesViewModel';
import { withViewModel } from 'MVVM/withViewModel';
import { Bookmark } from './Bookmark/Bookmark';
import { QuerySidebar } from './QuerySidebar';
import { Button } from 'primereact/button';
import css from './Queries.module.css';

const initialQueries = [
    { title: 'Query 1', id: '1' },
    { title: 'Query 2', id: '2' },
];

export const Queries = withViewModel(QueriesViewModel, () => {
    const op = useRef<OverlayPanel>(null);
    const [currentQuery, setCurrentQuery] = useState(1);
    const [queries, setQueries] = useState<QueryType[]>(initialQueries);

    const addQuery = useCallback(() => {
        setQueries((prevQueries) => {
            const newQueryIdx = prevQueries.length + 1;
            return [
                ...prevQueries,
                { title: `Query ${newQueryIdx}`, id: `${newQueryIdx}`, isEditing: false },
            ];
        });
    }, []);

    const onQueryChange = useCallback((e: ChangeEvent<HTMLInputElement>, idx: number) => {
        setQueries((prevQueries) =>
            prevQueries.map((query, index) =>
                index === idx ? { ...query, title: e.target.value } : query
            )
        );
    }, []);

    const toggleEdit = useCallback((idx: number) => {
        setQueries((prevQueries) =>
            prevQueries.map((query, index) =>
                index === idx ? { ...query, isEditing: !query.isEditing } : query
            )
        );
    }, []);

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
                onTabChange={(evt) => setCurrentQuery(evt.index)}
            >
                <TabPanel
                    headerTemplate={
                        <div className={css.buttonContainer}>
                            <Button
                                icon='pi pi-book'
                                onClick={(evt) => op.current?.toggle(evt)}
                            />
                            <OverlayPanel ref={op}>
                                <h1>Queries</h1>
                                <Bookmark />
                            </OverlayPanel>
                        </div>
                    }
                />
                {queries.map((query, idx) => (
                    <TabPanel
                        className={css.activeTab}
                        key={query.id}
                        closable={idx !== 0}
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
                            <QuerySidebar>
                                {query.title}
                                <div>Logs</div>
                                <div>Outbox</div>
                                <div>People</div>
                            </QuerySidebar>
                            <div>
                                <QueryMenuActions />
                            </div>
                        </div>
                    </TabPanel>
                ))}
                <TabPanel
                    headerTemplate={<Button icon='pi pi-plus' onClick={addQuery} />}
                />
            </TabView>
        </div>
    );
});
