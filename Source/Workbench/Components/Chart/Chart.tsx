// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef } from 'react';
import {
    Chart as ChartJs,
    type ChartConfiguration,
    type ChartData,
    type ChartOptions,
    type ChartType,
    registerables
} from 'chart.js';

ChartJs.register(...registerables);

/**
 * Props for {@link Chart}.
 */
export interface ChartProps {
    /** The chart.js chart type, e.g. `'line'`, `'bar'`, `'doughnut'`. */
    type: ChartType;
    /** The chart.js dataset configuration. */
    data: ChartData;
    /** The chart.js options. */
    options?: ChartOptions;
    /** Applied to the canvas element. */
    className?: string;
}

/**
 * Renders a chart.js chart.
 *
 * PrimeReact 10 shipped a `primereact/chart` wrapper around chart.js; PrimeReact 11
 * dropped it entirely. `chart.js` was already a direct dependency (it was the
 * wrapper's own peer), so the Workbench now drives it directly and keeps the
 * `type` / `data` / `options` call shape the dashboard widgets were written against.
 */
export const Chart = ({ type, data, options, className }: ChartProps) => {
    const canvasRef = useRef<HTMLCanvasElement>(null);
    const chartRef = useRef<ChartJs | null>(null);

    useEffect(() => {
        if (!canvasRef.current) return;

        chartRef.current = new ChartJs(canvasRef.current, { type, data, options } as ChartConfiguration);

        return () => {
            chartRef.current?.destroy();
            chartRef.current = null;
        };
    }, [type, data, options]);

    return <canvas ref={canvasRef} className={className} />;
};
