/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { EventTypeRegistration } from '../Events/EventTypeRegistration';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/types/schemas');

class AllEventTypesWithSchemasSortBy {
    private _type: SortingActionsForQuery<EventTypeRegistration[]>;
    private _schema: SortingActionsForQuery<EventTypeRegistration[]>;

    constructor(readonly query: AllEventTypesWithSchemas) {
        this._type = new SortingActionsForQuery<EventTypeRegistration[]>('type', query);
        this._schema = new SortingActionsForQuery<EventTypeRegistration[]>('schema', query);
    }

    get type(): SortingActionsForQuery<EventTypeRegistration[]> {
        return this._type;
    }
    get schema(): SortingActionsForQuery<EventTypeRegistration[]> {
        return this._schema;
    }
}

class AllEventTypesWithSchemasSortByWithoutQuery {
    private _type: SortingActions  = new SortingActions('type');
    private _schema: SortingActions  = new SortingActions('schema');

    get type(): SortingActions {
        return this._type;
    }
    get schema(): SortingActions {
        return this._schema;
    }
}

export interface AllEventTypesWithSchemasParameters {
    eventStore: string;
}

export class AllEventTypesWithSchemas extends QueryFor<EventTypeRegistration[], AllEventTypesWithSchemasParameters> {
    readonly route: string = '/api/event-store/{eventStore}/types/schemas';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: EventTypeRegistration[] = [];
    private readonly _sortBy: AllEventTypesWithSchemasSortBy;
    private static readonly _sortBy: AllEventTypesWithSchemasSortByWithoutQuery = new AllEventTypesWithSchemasSortByWithoutQuery();

    constructor() {
        super(EventTypeRegistration, true);
        this._sortBy = new AllEventTypesWithSchemasSortBy(this);
    }

    get requiredRequestParameters(): string[] {
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

    static use(args?: AllEventTypesWithSchemasParameters, sorting?: Sorting): [QueryResultWithState<EventTypeRegistration[]>, PerformQuery<AllEventTypesWithSchemasParameters>, SetSorting] {
        return useQuery<EventTypeRegistration[], AllEventTypesWithSchemas, AllEventTypesWithSchemasParameters>(AllEventTypesWithSchemas, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllEventTypesWithSchemasParameters, sorting?: Sorting): [QueryResultWithState<EventTypeRegistration[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<EventTypeRegistration[], AllEventTypesWithSchemas>(AllEventTypesWithSchemas, new Paging(0, pageSize), args, sorting);
    }
}
