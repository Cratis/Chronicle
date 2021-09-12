// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as d3 from 'd3';
import { SvgInstance } from './SvgInstance';

export class PieText {
    private readonly text: d3.Selection<any, any, any, any>;

    constructor(private readonly _svgInstance: SvgInstance) {
        this.text = _svgInstance.svg.append('text').style('pointer-events', 'none');
    }

    render(label, type, centeredLabelFontSize) {
        this.text.text('');
        const svgWidth = this._svgInstance.svg.style('width').replace('px', '');
        this.text
            .attr('dx', type.getTextCenteredX(svgWidth))
            .attr('dy', type.getTextCenteredY(svgWidth))
            .style('font-size', centeredLabelFontSize)
            .style('text-anchor', type.getTextCenteredAnchor)
            .style('opacity', 0)
            .text(label)
            .transition()
            .style('opacity', 1);
    }

    hide() {
        this.text.transition().style('opacity', 0);
    }
}

