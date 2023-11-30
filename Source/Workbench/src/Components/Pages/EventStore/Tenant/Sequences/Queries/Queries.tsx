// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TabView, TabPanel } from 'primereact/tabview';
import { QueryHeader, QueryType } from './QueryHeader';
import { QueryMenuActions } from './QueryMenuActions';
import { QueriesViewModel } from './QueriesViewModel';
import { withViewModel } from 'MVVM/withViewModel';
import { QuerySidebar } from './QuerySidebar';
import { ChangeEvent, useState } from 'react';
import { Button } from 'primereact/button';
import css from './Queries.module.css';

export const Queries = withViewModel(QueriesViewModel, () => {
    const [queries, setQueries] = useState<QueryType[]>([
        { title: 'Query 1', id: '1' },
        { title: 'Query 2', id: '2' },
    ]);
    const [currentQuery, setCurrentQuery] = useState(0);

    const addQuery = () => {
        const newQueryIdx = queries.length + 1;
        const newQuery = {
            title: `Query ${newQueryIdx}`,
            id: `${newQueryIdx}`,
            isEditing: false,
        };
        setQueries([...queries, newQuery]);
    };

    const onQueryChange = (e: ChangeEvent<HTMLInputElement>, idx: number) => {
        const updatedQueries = queries.map((query, index) =>
            index === idx ? { ...query, title: e.target.value } : query
        );
        setQueries(updatedQueries);
    };

    const toggleEdit = (idx: number) => {
        const updatedQueries = queries.map((query, index) =>
            index === idx ? { ...query, isEditing: !query.isEditing } : query
        );
        setQueries(updatedQueries);
    };


    return (
        <div className={css.container}>
            <div className={css.buttonContainer}>
                <Button icon='pi pi-book' />
                <Button icon='pi pi-plus' onClick={addQuery} />
            </div>
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
            </TabView>
        </div>
    );
});
