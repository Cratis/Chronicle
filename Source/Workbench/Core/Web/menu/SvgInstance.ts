// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as d3 from 'd3';
import { CircularMenuConfig } from './CircularMenuConfig';

export type RenderResult = {
    svg: d3.Selection<any, any, any, any>;
    container?: d3.Transition<any, any, any, any>;
};

export class SvgInstance {
    private readonly _el: d3.Selection<any, any, any, any>;
    private _width = 0;
    private _height = 0;
    readonly svg: d3.Selection<any, any, any, any>;
    container: d3.Selection<any, any, any, any>;

    constructor(private readonly element: Element, private readonly cfg: CircularMenuConfig) {
        // SVG Creation
        this._el = d3.select(element);
        this.svg = this._el.append('svg').style('background-color', cfg.backgroundColor);
        this.container = this.svg.append('svg:g');
    }

    render(data: any[], config: CircularMenuConfig): RenderResult {
        const type = config.type;
        const backgroundColor = config.backgroundColor;

        if (config.width) {
            // fixed width
            this._width = config.width;
        } else {
            // responsive
            const parentNodes = d3.select(this.element.parentElement).nodes();
            if (parentNodes && parentNodes.length > 0) {
                const parentNode = parentNodes[0] as HTMLElement;
                const parentWidth = parentNode.offsetWidth;
                this._width = parentWidth;
            }
        }
        // background color
        this.svg.style('background-color', backgroundColor);

        // svg and container position in according with the menu TYPE
        if (type) {
            this._height = type.getHeight(this._width);
            const container = this._el.select('svg')
                .data([data])
                .transition()
                .duration(500)
                .attr('width', this._width)
                .attr('height', this._height)
                .select('g')
                .attr('transform', 'translate(' + type.getPos(this._width).x + ',' + type.getPos(this._width).y + ')');

            return {
                svg: this.svg,
                container
            };
        }

        return {
            svg: this.svg
        };
    }
}
