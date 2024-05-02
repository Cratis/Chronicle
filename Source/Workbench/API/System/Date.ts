// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { DayOfWeek } from './DayOfWeek';
import { TimeSpan } from './TimeSpan';

export class Date {

    @field(Date)
    dateTime!: Date;

    @field(Date)
    utcDateTime!: Date;

    @field(Date)
    localDateTime!: Date;

    @field(Date)
    date!: Date;

    @field(Number)
    day!: number;

    @field(DayOfWeek)
    dayOfWeek!: DayOfWeek;

    @field(Number)
    dayOfYear!: number;

    @field(Number)
    hour!: number;

    @field(Number)
    millisecond!: number;

    @field(Number)
    microsecond!: number;

    @field(Number)
    nanosecond!: number;

    @field(Number)
    minute!: number;

    @field(Number)
    month!: number;

    @field(TimeSpan)
    offset!: TimeSpan;

    @field(Number)
    totalOffsetMinutes!: number;

    @field(Number)
    second!: number;

    @field(Number)
    ticks!: number;

    @field(Number)
    utcTicks!: number;

    @field(TimeSpan)
    timeOfDay!: TimeSpan;

    @field(Number)
    year!: number;
}
