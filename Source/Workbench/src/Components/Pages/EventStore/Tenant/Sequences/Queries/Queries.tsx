// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TabView, TabPanel } from 'primereact/tabview';
import { QueryHeader, QueryType } from './QueryHeader';
import { QueriesViewModel } from './QueriesViewModel';
import { withViewModel } from 'MVVM/withViewModel';
import { ChangeEvent, useState } from 'react';
import { Button } from 'primereact/button';
import css from './Queries.module.css';
import { QuerySidebar } from './QuerySidebar';

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
        };
        setQueries([...queries, newQuery]);
    };

    const onQueryChange = (e: ChangeEvent<HTMLInputElement>, idx: number) => {
        const updatedQueries = queries.map((query, index) =>
            index === idx ? { ...query, title: e.target.value } : query
        );
        setQueries(updatedQueries);
    };

    return (
        <div className={css.container}>
            <TabView
                scrollable
                className={css.tabView}
                activeIndex={currentQuery}
                onTabChange={(e) => setCurrentQuery(e.index)}
            >
                {queries.map((query, idx) => (
                    <TabPanel
                        key={query.id}
                        closable={idx !== 0}
                        header={
                            <div className={css.panelHeader}>
                                <Button
                                    style={{
                                        visibility: idx === 0 ? 'visible' : 'hidden',
                                    }}
                                    icon='pi pi-plus'
                                    onClick={addQuery}
                                    className={css.tabButton}
                                />
                                <QueryHeader
                                    idx={idx}
                                    query={query}
                                    onQueryChange={onQueryChange}
                                />
                            </div>
                        }
                    >
                        <QuerySidebar>
                            {query.title}
                            <br />
                            sidebar content goes here
                            sidebar content goes here
                            sidebar content goes here
                            sidebar content goes here
                            sidebar content goes here
                        </QuerySidebar>
                    </TabPanel>
                ))}
            </TabView>
        </div>
    );
});
