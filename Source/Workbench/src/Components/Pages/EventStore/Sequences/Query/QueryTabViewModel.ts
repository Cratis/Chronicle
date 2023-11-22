import { makeAutoObservable } from 'mobx';

export class QueryTabViewModel {

    tabs = [
        { title: 'Query 1', content: 'Tab 1 Content' },
        { title: 'Query 2', content: 'Tab 2 Content' },
        { title: 'Query 3', content: 'Tab 3 Content' }
    ];
    activeIdx = 0;

    constructor() {
        makeAutoObservable(this);
    }

    setActiveIdx(idx: number) {
        this.activeIdx = idx;
    }

    addTab() {
        const newTabIndex = this.tabs.length + 1;
        const newTab = {
            title: `Query ${newTabIndex}`,
            content: `Tab ${newTabIndex} Content`
        };
        this.tabs.push(newTab);
    }




    panelClassName(idx: number) {
        return this.activeIdx === idx ? 'bg-primary' : '';
    }
}
