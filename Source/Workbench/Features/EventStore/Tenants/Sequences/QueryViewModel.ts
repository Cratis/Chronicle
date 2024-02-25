import { QueryDefinition } from './QueryDefinition';

export class QueryViewModel {
    private _query!: QueryDefinition;
    private _hasChanges: boolean = false;

    constructor() {
    }

    get query(): QueryDefinition {
        return this._query;
    }

    set query(query: QueryDefinition) {
        this._query = query;
    }

    get hasChanges(): boolean {
        return this._hasChanges;
    }

    changeSomething() {
        this._hasChanges = true;
    }

    save() {
        this._hasChanges = false;
    }
}
