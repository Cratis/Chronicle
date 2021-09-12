// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as d3 from 'd3';
import * as PieTypes from './PieTypes';
import { SvgInstance } from './SvgInstance';
import { ItemClicked, ItemMouseOut, ItemMouseOver } from './CallbackTypes';
import { CircularMenuConfig } from './CircularMenuConfig';

const bg = d3.schemeCategory10;

export class Pie {
    readonly pie: d3.Pie<any, any>;
    readonly arc: d3.Arc<any, any>;
    readonly arcText: d3.Arc<any, any>;
    private _data: any[] = [];
    private _config!: CircularMenuConfig;
    private _itemClicked!: ItemClicked;
    private _itemMouseOver!: ItemMouseOver;
    private _itemMouseOut!: ItemMouseOut;
    private _current: any;

    constructor(private readonly _svgInstance: SvgInstance) {
        this.arc = d3.arc();
        this.arcText = d3.arc();
        this.pie = d3.pie().value((d: any, i: number, data: any[]) => {
            return d.value;
        });
    }

    /**
     * Render function (when data changes or is resized)
     */
    render(data: any[], config: CircularMenuConfig, itemClicked: ItemClicked, itemMouseOver: ItemMouseOver, itemMouseOut: ItemMouseOut) {
        this._data = data;
        this._config = config;
        this._itemClicked = itemClicked;
        this._itemMouseOver = itemMouseOver;
        this._itemMouseOut = itemMouseOut;

        this.pie.value(function pieValue(d) {
            return d.value;
        }).sort(null).startAngle(config.type.startAngle).endAngle(config.type.endAngle);

        this.renderSegments();
        this.renderLabels();
    }


    // -------------------------------------------------------
    // TWEENS functions
    // -------------------------------------------------------
    // Store the displayed angles in _current.
    // Then, interpolate from _current to the new angles.
    // During the transition, _current is updated in-place by d3.interpolate.
    private arcTween(a) {
        const i = d3.interpolate(this._current, a);
        this._current = i(0);
        return (t) => {
            return this.arc(i(t));
        };
    }

    // Interpolate exiting arcs start and end angles to Math.PI * 2
    // so that they 'exit' at the end of the data
    private arcTweenOut() {
        const i = d3.interpolate(this._current, { startAngle: Math.PI * 2, endAngle: Math.PI * 2, value: 0 });
        this._current = i(0);
        return (t) => {
            return this.arc(i(t));
        };
    }

    // Tween used to animated the radius
    private tweenRadius(b) {
        return (a, i) => {
            const d = b.call(this, a, i);
            const _i = d3.interpolate(a, d);
            for (const k in d) {
                if (a) {
                    a[k] = d[k];
                }
            }
            return (t) => {
                return this.arc(_i(t));
            };
        };
    }

    // -------------------------------------------------------
    // DRAW functions
    // -------------------------------------------------------

    /**
     * Render ARC segments
     */
    private renderSegments() {
        const svgWidth = this._svgInstance.svg.style('width').replace('px', '');
        const w = this._config.type.getWidth(parseFloat(svgWidth));
        // arc.outerRadius(w)
        //    .innerRadius(w - config.pieSize);
        this.arcText.outerRadius(w).innerRadius(w - this._config.pieSize);
        // path segments join
        const path = this._svgInstance.container.selectAll('path').data(this.pie(this._data));

        const factory = this.tweenRadius((a,i) => {
            return {
                startAngle: a.startAngle,
                endAngle: a.endAngle,
                innerRadius: w,
                outerRadius: w - this._config.pieSize,
            };
        });
        path.transition().duration(1000).attrTween('d', factory as any);

        // enter selection
        path.enter()
            .append('path')
            .classed('menu-circular-segment', true)
            .attr('d', this.arc(PieTypes.enterAntiClockwise) as any)
            .attr('fill', 'white').style('opacity', 0)
            // set the start and end angles to Math.PI * 2 so we can transition
            // anticlockwise to the actual values
            .each((d) => {
                this._current = {
                    data: d.data,
                    value: d.value,
                    startAngle: PieTypes.enterAntiClockwise.startAngle,
                    endAngle: PieTypes.enterAntiClockwise.endAngle,
                    outerRadius: w,
                    innerRadius: w - this._config.pieSize
                };
            }). //  arc: arc,
            on('click', (e, d) => {
                // Todo
                // d3.event.stopPropagation();
                this._itemClicked(d.data);
            }).on('mouseover', (e, d) => {
                // Todo
                //d3.event.stopPropagation();
                //d3.select(this).style('opacity', 0.7).style('cursor', 'pointer');
                this._itemMouseOver(d.data);
            }).on('mouseout', (e, d) => {
                // Todo
                //d3.event.stopPropagation();
                //d3.select(this).style('opacity', 1).style('cursor', 'auto');
                this._itemMouseOut(d.data);
            });

        // Exit selection
        path.exit().transition().duration(750).attrTween('d', this.arcTweenOut.bind(this) as any).remove();

        // Update selection
        path.transition().duration(750).attrTween('d', this.arcTween.bind(this) as any).style('opacity', 1).attr('fill', (d, i) => {
            const dt = this._data[i];
            return dt.bg ? dt.bg : bg[i];
        });
    }

    /**
     * Render Labels
     */
    private renderLabels() {
        // join
        const texts = this._svgInstance.container.selectAll('text').data(this.pie(this._data));

        // enter selection
        texts.enter().append('text').style('opacity', 0).attr('text-anchor', () => {
            return this._config.type.textAnchor;
        }).style('fill', (d, i) => {
            const dt = this._data[i];
            return dt.color ? dt.color : this._config.colorIcon;
        });

        // Update selection
        texts.style('pointer-events', 'none').attr('font-family', 'FontAwesome')
            // .attr('font-size', function(d) { return d.size+'em'} )
            .attr('font-size', () => {
                return this._config.sizeIcon;
            }).text((d, i) => {
                const dt = this._data[i];
                return this._config.showIcon ? dt.icon : dt.label;
            }).transition().duration(760).delay((d, i) => {
                return i * 100;
            }).attr('dy', () => {
                return this._config.type.textY;
            }).style('fill', (d, i) => {
                const dt = this._data[i];
                return dt.color ? dt.color : '#555';
            }).style('opacity', 1).attr('transform', (d) => {
                return 'translate(' + this.arcText.centroid(d) + ')';
            });

        // Exit selection
        texts.exit().transition().duration(750).style('opacity', 0).remove();
    }
}

