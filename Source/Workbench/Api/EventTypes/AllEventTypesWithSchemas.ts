/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { EventTypeWithSchemas } from './EventTypeWithSchemas';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{eventStore}}/types/schemas');

class AllEventTypesWithSchemasSortBy {
    private _type: SortingActionsForQuery<EventTypeWithSchemas[]>;
    private _schemas: SortingActionsForQuery<EventTypeWithSchemas[]>;

    constructor(readonly query: AllEventTypesWithSchemas) {
        this._type = new SortingActionsForQuery<EventTypeWithSchemas[]>('type', query);
        this._schemas = new SortingActionsForQuery<EventTypeWithSchemas[]>('schemas', query);
    }

    get type(): SortingActionsForQuery<EventTypeWithSchemas[]> {
        return this._type;
    }
    get schemas(): SortingActionsForQuery<EventTypeWithSchemas[]> {
        return this._schemas;
    }
}

class AllEventTypesWithSchemasSortByWithoutQuery {
    private _type: SortingActions  = new SortingActions('type');
    private _schemas: SortingActions  = new SortingActions('schemas');

    get type(): SortingActions {
        return this._type;
    }
    get schemas(): SortingActions {
        return this._schemas;
    }
}

export interface AllEventTypesWithSchemasArguments {
    eventStore: string;
}

export class AllEventTypesWithSchemas extends QueryFor<EventTypeWithSchemas[], AllEventTypesWithSchemasArguments> {
    readonly route: string = '/api/events/store/{eventStore}/types/schemas';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventTypeWithSchemas[] = [];
    private readonly _sortBy: AllEventTypesWithSchemasSortBy;
    private static readonly _sortBy: AllEventTypesWithSchemasSortByWithoutQuery = new AllEventTypesWithSchemasSortByWithoutQuery();

    constructor() {
        super(EventTypeWithSchemas, true);
        this._sortBy = new AllEventTypesWithSchemasSortBy(this);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
        ];
    }

    get sortBy(): AllEventTypesWithSchemasSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllEventTypesWithSchemasSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AllEventTypesWithSchemasArguments, sorting?: Sorting): [QueryResultWithState<EventTypeWithSchemas[]>, PerformQuery<AllEventTypesWithSchemasArguments>, SetSorting] {
        return useQuery<EventTypeWithSchemas[], AllEventTypesWithSchemas, AllEventTypesWithSchemasArguments>(AllEventTypesWithSchemas, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllEventTypesWithSchemasArguments, sorting?: Sorting): [QueryResultWithState<EventTypeWithSchemas[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<EventTypeWithSchemas[], AllEventTypesWithSchemas>(AllEventTypesWithSchemas, new Paging(0, pageSize), args, sorting);
    }
}
