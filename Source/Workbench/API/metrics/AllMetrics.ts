/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from '@aksio/applications/queries';
import { MetricMeasurement } from './MetricMeasurement';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/metrics');

export class AllMetrics extends ObservableQueryFor<MetricMeasurement[]> {
    readonly route: string = '/api/metrics';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: MetricMeasurement[] = [];

    constructor() {
        super(MetricMeasurement, true);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResultWithState<MetricMeasurement[]>] {
        return useObservableQuery<MetricMeasurement[], AllMetrics>(AllMetrics);
    }
}
