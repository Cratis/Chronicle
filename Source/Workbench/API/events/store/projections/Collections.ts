/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import { ProjectionCollection } from './ProjectionCollection';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{microserviceId}/projections/{{projectionId}}/collections?microserviceId={{microserviceId}}');

export interface CollectionsArguments {
    microserviceId: string;
    projectionId: string;
}
export class Collections extends QueryFor<ProjectionCollection[], CollectionsArguments> {
    readonly route: string = '/api/events/store/{microserviceId}/projections/{{projectionId}}/collections?microserviceId={{microserviceId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ProjectionCollection[] = [];

    constructor() {
        super(ProjectionCollection, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'projectionId',
        ];
    }

    static use(args?: CollectionsArguments): [QueryResultWithState<ProjectionCollection[]>, PerformQuery<CollectionsArguments>] {
        return useQuery<ProjectionCollection[], Collections, CollectionsArguments>(Collections, args);
    }
}
