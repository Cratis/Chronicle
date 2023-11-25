// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AddFilter } from 'Components/Filters/AddFilter/AddFilter';
import { QueriesViewModel } from './QueriesViewModel';
import { TabView, TabPanel } from 'primereact/tabview';
import { Button } from 'primereact/button';
import css from './Queries.module.css';
import { withViewModel } from 'MVVM/withViewModel';

export const Queries = withViewModel(QueriesViewModel, ({ viewModel }) => {
    return (
        <div className={css}>
            <div className={css.tabContainer}>
                <Button
                    icon='pi pi-plus'
                    label='Add new query'
                    className={css.addButton}
                    onClick={() => viewModel.addQuery()}
                />
                <TabView
                    className={css.tabView}
                    activeIndex={viewModel.currentQuery}
                    onTabChange={(e) => viewModel.setCurrentQuery(e.index)}
                >
                    {viewModel.queries.map((query, idx) => (
                        <TabPanel
                            key={idx}
                            closable={idx !== 0}
                            header={query.title}
                            className={viewModel.panelClassName(idx)}
                        >
                            <>another component goes here {query.id}</>
                            <AddFilter />
                        </TabPanel>
                    ))}
                </TabView>
            </div>
        </div>
    );
});
