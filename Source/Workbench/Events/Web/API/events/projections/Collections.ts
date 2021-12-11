/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/frontend/queries';
import { ProjectionCollection } from './ProjectionCollection';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/projections/{{projectionId}}/collections');

export interface CollectionsArguments {
    projectionId: string;
}
export class Collections extends QueryFor<ProjectionCollection[], CollectionsArguments> {
    readonly route: string = '/api/events/projections/{{projectionId}}/collections';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ProjectionCollection[] = [];
    readonly requiresArguments: boolean = true;

    static use(args?: CollectionsArguments): [QueryResult<ProjectionCollection[]>, PerformQuery<CollectionsArguments>] {
        return useQuery<ProjectionCollection[], Collections, CollectionsArguments>(Collections, args);
    }
}
