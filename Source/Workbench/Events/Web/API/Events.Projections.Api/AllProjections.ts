/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResult, useObservableQuery } from '@aksio/frontend/queries';
import { Projection } from './Projection';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/projections');

export class AllProjections extends ObservableQueryFor<Projection[]> {
    readonly route: string = '/api/events/projections';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Projection[] = [];
    readonly requiresArguments: boolean = false;

    static use(): [QueryResult<Projection[]>] {
        return useObservableQuery<Projection[], AllProjections>(AllProjections);
    }
}
