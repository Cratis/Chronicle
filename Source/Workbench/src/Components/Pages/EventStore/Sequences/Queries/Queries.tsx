// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AddFilter } from 'Components/Filters/AddFilter/AddFilter';
import { QueriesViewModel } from './QueriesViewModel';
import { TabView, TabPanel } from 'primereact/tabview';
import { Button } from 'primereact/button';
import css from './Queries.module.css';
import { observer } from 'mobx-react';
import { container } from 'tsyringe';
import { AddFilterViewModel } from '../../../../Filters/AddFilter/AddFilterViewModel';
export interface QueriesProps {
    viewModel: QueriesViewModel;
}

export const Queries = observer((props: QueriesProps) => {
    const { viewModel } = props;

    return (
        <div className={css}>
            <div className={css.tabContainer}>
                <Button
                    icon='pi pi-plus'
                    label='Add new Tab'
                    className={css.addButton}
                    onClick={() => viewModel.addTab()}
                />
                <TabView
                    className={css.tabView}
                    activeIndex={viewModel.activeIdx}
                    onTabChange={(e) => viewModel.setActiveIdx(e.index)}
                >
                    {viewModel.queries.map((query, idx) => (
                        <TabPanel
                            key={idx}
                            closable={idx !== 0}
                            header={query.title}
                            className={viewModel.panelClassName(idx)}
                        >
                            <>another component goes here {query.id}</>
                            <AddFilter
                                viewModel={container.resolve(AddFilterViewModel)}
                            />
                        </TabPanel>
                    ))}
                </TabView>
            </div>
        </div>
    );
});
