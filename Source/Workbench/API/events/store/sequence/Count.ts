/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/sequence/{eventSequenceId}/count');

export class Count extends QueryFor<number> {
    readonly route: string = '/api/events/store/sequence/{eventSequenceId}/count';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: number = {} as any;

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResult<number>, PerformQuery] {
        return useQuery<number, Count>(Count);
    }
}
