// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* eslint-disable react/no-find-dom-node */

import { useEffect, useState } from 'react';
import * as d3 from 'd3';
import * as PieTypes from './PieTypes';
import { PieText } from './PieText';
import { Pie } from './Pie';
import { RenderResult, SvgInstance } from './SvgInstance';
import { CircularMenuConfig } from './CircularMenuConfig';
import { ItemClicked, ItemMouseOut, ItemMouseOver } from './CallbackTypes';
import { CircularMenuItem } from './CircularMenuItem';


export const DEFAULT_CONFIG = {
    type: PieTypes.HALF,
    colors: [],
    width: null,
    showIcon: false,
    sizeIcon: '2em',
    innerRadius: 50,
    showCenteredLabel: true,
    centeredLabelFontSize: '1.5em',
    backgroundColor: 'rgba(255, 255, 255, 0)'
};

export interface CircularMenuProps {
    style?: any,
    data: CircularMenuItem[];
    config: CircularMenuConfig;
    show: boolean;
    // innerRadius: PropTypes.number,
    onItemClick?: ItemClicked,
    onItemMouseOver?: ItemMouseOver,
    onItemMouseOut?: ItemMouseOut
}

export const CircularMenu = (props: CircularMenuProps) => {
    let root: HTMLDivElement | null;
    const [svgInstance, setSvgInstance] = useState<SvgInstance>();
    const [pie, setPie] = useState<Pie>();
    const [text, setText] = useState<PieText>();

    const data = props.data;

    const configuration = { ...DEFAULT_CONFIG, ...props.config };

    const render = (svgInstance: SvgInstance, pie: Pie, text: PieText) => {
        if (data) {
            svgInstance.render(data, configuration);
            pie.render(data, configuration,
                (d) => {
                    props.onItemClick?.(d);
                },
                (d) => {
                    if (configuration.showCenteredLabel) {
                        text!.render(d.label, configuration.type, configuration.centeredLabelFontSize);
                        props.onItemMouseOver?.(d);
                    }
                },
                (d) => {
                    text!.hide();
                    props.onItemMouseOut?.(d);
                });
        }
    };

    useEffect(() => {
        const svgInstance = new SvgInstance(root!, configuration);
        setSvgInstance(svgInstance);
        const pie = new Pie(svgInstance);
        setPie(pie);
        const text = new PieText(svgInstance);
        setText(text);

        if (data) {
            render(svgInstance, pie, text);
        }
        const ns = new Date().valueOf();
        d3.select(window).on('resize.' + ns, () => {
            if (props.show) {
                render(svgInstance, pie, text);
            }
        });
    }, []);

    if (data && svgInstance && pie && text) {
        render(svgInstance, pie, text);
    }

    return (
        <div ref={node => {
            root = node;
        }} style={...props.style} />
    );
};
