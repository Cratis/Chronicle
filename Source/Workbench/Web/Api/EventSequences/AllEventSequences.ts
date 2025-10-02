/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/sequences');

class AllEventSequencesSortBy {

    constructor(readonly query: AllEventSequences) {
    }

}

class AllEventSequencesSortByWithoutQuery {

}

export interface AllEventSequencesParameters {
    eventStore: string;
}

export class AllEventSequences extends QueryFor<string[], AllEventSequencesParameters> {
    readonly route: string = '/api/event-store/{eventStore}/sequences';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: string[] = [];
    private readonly _sortBy: AllEventSequencesSortBy;
    private static readonly _sortBy: AllEventSequencesSortByWithoutQuery = new AllEventSequencesSortByWithoutQuery();

    constructor() {
        super(String, true);
        this._sortBy = new AllEventSequencesSortBy(this);
    }

    get requiredRequestParameters(): string[] {
        return [
            'eventStore',
        ];
    }

    get sortBy(): AllEventSequencesSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllEventSequencesSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AllEventSequencesParameters, sorting?: Sorting): [QueryResultWithState<string[]>, PerformQuery<AllEventSequencesParameters>, SetSorting] {
        return useQuery<string[], AllEventSequences, AllEventSequencesParameters>(AllEventSequences, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllEventSequencesParameters, sorting?: Sorting): [QueryResultWithState<string[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<string[], AllEventSequences>(AllEventSequences, new Paging(0, pageSize), args, sorting);
    }
}
