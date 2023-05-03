/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { MetricMeasurementPoint } from './MetricMeasurementPoint';

export class MetricMeasurement {

    @field(String)
    name!: string;

    @field(Number)
    aggregated!: number;

    @field(MetricMeasurementPoint, true)
    points!: MetricMeasurementPoint[];
}
