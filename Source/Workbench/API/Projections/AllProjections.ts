/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { Projection } from './Projection';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{eventStore}/projections');

export interface AllProjectionsArguments {
    eventStore: string;
}
export class AllProjections extends QueryFor<Projection[], AllProjectionsArguments> {
    readonly route: string = '/api/events/store/{eventStore}/projections';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Projection[] = [];

    constructor() {
        super(Projection, true);
    }

    get requestArguments(): string[] {
        return [
            'eventStore',
        ];
    }

    static use(args?: AllProjectionsArguments): [QueryResultWithState<Projection[]>, PerformQuery<AllProjectionsArguments>] {
        return useQuery<Projection[], AllProjections, AllProjectionsArguments>(AllProjections, args);
    }
}
