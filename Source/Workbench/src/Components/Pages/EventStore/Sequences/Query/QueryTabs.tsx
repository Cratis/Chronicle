import { QueryTabViewModel } from './QueryTabViewModel';
import { TabView, TabPanel } from 'primereact/tabview';
import { Button } from 'primereact/button';
import { observer } from 'mobx-react';
export interface QueryTabsProps {
    viewModel: QueryTabViewModel;
}

export const QueryTabs = observer((props: QueryTabsProps) => {
    const { viewModel } = props;
    return (
        <div className='card'>
            <Button
                className='mb-3'
                label='Add new Tab'
                onClick={() => viewModel.addTab()}
            />
            <TabView
                activeIndex={viewModel.activeIdx}
                onTabChange={(e) => viewModel.setActiveIdx(e.index)}
            >
                {viewModel.tabs.map((tab, i) => (
                    <TabPanel
                        closable={i !== 0}
                        key={i}
                        header={tab.title}
                        className={viewModel.panelClassName(i)}
                    >
                        <p className='m-0'>{tab.content}</p>
                    </TabPanel>
                ))}
            </TabView>
        </div>
    );
});
