import { makeAutoObservable } from 'mobx';

export class QueriesViewModel {

    queries = [
        { title: 'Query 1', id: '1' },
        { title: 'Query 2', id: '2' },
        { title: 'Query 3', id: '3' }
    ];


    activeIdx = 0;

    constructor() {
        makeAutoObservable(this);
    }

    setActiveIdx(idx: number) {
        this.activeIdx = idx;
    }

    addTab() {
        const newQueryIdx = this.queries.length + 1;
        const newQuery = {
            title: `Query ${newQueryIdx}`,
            id: ` ${newQueryIdx} Content id`
        };
        this.queries.push(newQuery);
    }

    panelClassName(idx: number) {
        return this.activeIdx === idx ? 'bg-primary' : '';
    }
}
