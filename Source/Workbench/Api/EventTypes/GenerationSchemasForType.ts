/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{eventStore}}/types/schemas/{{eventTypeId}}');

class GenerationSchemasForTypeSortBy {

    constructor(readonly query: GenerationSchemasForType) {
    }

}

class GenerationSchemasForTypeSortByWithoutQuery {

}

export interface GenerationSchemasForTypeArguments {
    eventStore: string;
    eventTypeId: string;
}

export class GenerationSchemasForType extends QueryFor<any[], GenerationSchemasForTypeArguments> {
    readonly route: string = '/api/events/store/{eventStore}/types/schemas/{eventTypeId}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: any[] = [];
    private readonly _sortBy: GenerationSchemasForTypeSortBy;
    private static readonly _sortBy: GenerationSchemasForTypeSortByWithoutQuery = new GenerationSchemasForTypeSortByWithoutQuery();

    constructor() {
        super(Object, true);
        this._sortBy = new GenerationSchemasForTypeSortBy(this);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
            'eventTypeId',
        ];
    }

    get sortBy(): GenerationSchemasForTypeSortBy {
        return this._sortBy;
    }

    static get sortBy(): GenerationSchemasForTypeSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: GenerationSchemasForTypeArguments, sorting?: Sorting): [QueryResultWithState<any[]>, PerformQuery<GenerationSchemasForTypeArguments>, SetSorting] {
        return useQuery<any[], GenerationSchemasForType, GenerationSchemasForTypeArguments>(GenerationSchemasForType, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: GenerationSchemasForTypeArguments, sorting?: Sorting): [QueryResultWithState<any[]>, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<any[], GenerationSchemasForType>(GenerationSchemasForType, new Paging(0, pageSize), args, sorting);
    }
}
