// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect, useRef } from 'react';
import { default as styles } from './CircularMenu.module.scss';
import { default as circleStyles } from './CircularMenuItemCircle.module.scss';
import { getRenderingItemsFor, RenderingItem } from './RenderingItem';
import { CircularMenuItemIcon } from './CircularMenuItemIcon';
import { ICircularMenuProps } from './ICircularMenuProps';
import { CircularMenuItemCircle } from './CircularMenuItemCircle';

const getOffset = (element: Element) => {
    const bound = element.getBoundingClientRect();
    const html = document.documentElement;

    return {
        top: bound.top + window.pageYOffset - html.clientTop,
        left: bound.left + window.pageXOffset - html.clientLeft
    };
};

let mouseX = 0;
let mouseY = 0;

export const CircularMenu = (props: ICircularMenuProps) => {
    const [current, setCurrent] = useState<ICircularMenuProps>();
    const [previous, setPrevious] = useState<ICircularMenuProps>();
    const [label, setLabel] = useState('');
    const [previousLabel, setPreviousLabel] = useState('');
    const [labelCrossFade, setLabelCrossFade] = useState(false);
    const svg = useRef<SVGSVGElement>(null);
    const [itemsForRendering, setItemsForRendering] = useState<RenderingItem[]>([]);

    const handleItemsForRendering = (current?: ICircularMenuProps, previous?: ICircularMenuProps) => {
        let items:RenderingItem[] = [];
        if (current) {
            items = getRenderingItemsFor(current, false);
        }
        if (previous && current !== previous) {
            items = [...items, ...getRenderingItemsFor(previous, true)];
        }

        setItemsForRendering(items);
    };


    const onClick = (event: MouseEvent) => {
        setLabel('');
        setPreviousLabel('');
        setLabelCrossFade(false);
        handleItemsForRendering(props, current);
    };

    const setLabelFromElement = (element: Element) => {
        let newLabel = '';
        if (element instanceof SVGElement) {
            const item = itemsForRendering.find(_ => _.identifier == ((element as SVGElement).id));
            if (item && item.label) {
                newLabel = item.label;
            }
        }
        setLabel(newLabel);
        if (label !== newLabel) {
            setLabelCrossFade(true);
        }
    }

    const onMouseMove = (event: MouseEvent) => {
        mouseX = event.clientX;
        mouseY = event.clientY;
        setLabelFromElement(event.target as any);
    };

    const onAnimationEnd = (event: AnimationEvent) => {
        if (event.animationName === styles.fadeOut) {
            setPreviousLabel(label);
            setLabelCrossFade(false);
        }
        if (event.animationName === circleStyles.slideOut) {
            if (current && current.items.length > 0) {
                setPrevious({ ...current, items: [] });
            }

            const element = document.elementFromPoint(mouseX, mouseY);
            if (element) {
                setLabelFromElement(element);
            }
        }
    };

    useEffect(() => {
        setPrevious(current);
        setCurrent(props);

        handleItemsForRendering(props, current);

        if (svg?.current) {
            svg.current.style.top = '130px';
        }
    }, [props.items]);

    return (
        <svg
            viewBox="0 0 40 40"
            width="300"
            ref={svg}
            onMouseMove={onMouseMove as any}
            onClick={onClick as any}
            onAnimationEnd={onAnimationEnd as any}
        >
            {itemsForRendering.map((item, index) => (
                <g key={item.identifier}>
                    <CircularMenuItemCircle {...item} />
                    <CircularMenuItemIcon {...item} />
                </g>
            ))}

            <text x="20"
                y="20"
                dominantBaseline="middle"
                textAnchor="middle"
                fontSize={3}
                fill="white"
                className={`${styles.label} ${labelCrossFade ? styles.enter : ''}`}
            >{label}</text>

            <text x="20"
                y="20"
                dominantBaseline="middle"
                textAnchor="middle"
                fontSize={3}
                fill="white"
                className={`${styles.previousLabel} ${labelCrossFade ? styles.exit : ''}`}
            >{previousLabel}</text>
        </svg>
    );
};
