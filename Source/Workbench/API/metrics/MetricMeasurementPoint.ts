/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { MetricMeasurementPointTag } from './MetricMeasurementPointTag';

export class MetricMeasurementPoint {

    @field(Number)
    value!: number;

    @field(MetricMeasurementPointTag, true)
    tags!: MetricMeasurementPointTag[];
}
