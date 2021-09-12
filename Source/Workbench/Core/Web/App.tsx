// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useRef, useEffect } from "react";
import { PrimaryButton, Icon } from '@fluentui/react';

import { Menu } from './Menu';

import './App.scss';
import { CircularMenu, ICircularMenuProps } from './CircularMenu';
import { Toolbar, ToolbarMenu, ToolbarButton, ToolbarDirection } from './Toolbar';


export const App = () => {
    const svg = useRef(null);
    useEffect(() => {
        //makeDraggable(svg.current);
    }, [svg]);

    const [currentMenu, setCurrentMenu] = useState<ICircularMenuProps>();

    const green = {
        name: 'green',
        color: 'green',
        items: [
            { icon: '12PointStar', label: '12Point', onClick: () => setCurrentMenu(topLevel) },
            { icon: '6PointStar', label: '6point', onClick: () => setCurrentMenu(red) },
            { icon: 'AccessibiltyChecker', label: 'Accessibility' },
            { icon: 'AccountActivity', iconColor: 'black', label: 'Activity' },
            { icon: 'AccountBrowser', iconColor: 'black', label: 'Account' },
            { icon: 'ChromeClose', iconColor: 'black', onClick: () => setCurrentMenu(topLevel) }
        ]
    } as ICircularMenuProps;

    const red = {
        name: 'red',
        color: 'red',
        items: [
            { icon: 'Add', label: 'Add', onClick: () => setCurrentMenu(topLevel) },
            { icon: '6PointStar', label: 'Star', onClick: () => setCurrentMenu(green) },
            { icon: 'AccountActivity', label: 'Activity' },
            { icon: 'AccountBrowser', label: 'Account' },
            { icon: 'Video', label: 'Video'},
            { icon: 'ChromeClose', onClick: () => setCurrentMenu(topLevel) }
        ]
    } as ICircularMenuProps;

    const topLevel = {
        name: 'top',
        color: 'blue',
        items: [
            { icon: 'Video', label: 'Video', onClick: () => setCurrentMenu(red) },
            { icon: 'Mail', label: 'Mail', onClick: () => setCurrentMenu(green) },
            { icon: 'AddOnlineMeeting', label: 'Meeting' },
            { icon: 'AddReaction', label: 'Reaction', iconColor: 'black', onClick: () => setCurrentMenu(green) }
        ]
    } as ICircularMenuProps;

    if (!currentMenu) {
        setCurrentMenu(green);
    }

    return (
        <div>
            <svg viewBox="0 0 30 20" ref={svg}>
            </svg>
            <div className="topLayer">
                <CircularMenu {...currentMenu!} />
                <Toolbar direction={ToolbarDirection.vertical} style={{ position: 'absolute', top: 'calc(50%)' }}>
                    <ToolbarButton icon="TouchPointer" tooltip="Interact with items" />
                    <ToolbarMenu icon="Add" tooltip="Add item">
                        <ToolbarButton icon="WebAppBuilderModule" tooltip="Add microservice" />
                        <ToolbarButton icon="GiftboxOpen" tooltip="Add resource" />
                    </ToolbarMenu>
                    <ToolbarButton icon="QuickNote" tooltip="Quick note" />
                </Toolbar>
                <Toolbar direction={ToolbarDirection.horizontal}>
                    <ToolbarButton icon="FunnelChart" tooltip="Navigation" />
                    <ToolbarButton icon="History" tooltip="History" />
                    <ToolbarButton icon="LightningBolt" tooltip="Events" />
                    <ToolbarButton icon="PlugSolid" tooltip="APIs" />
                </Toolbar>

                <Toolbar direction={ToolbarDirection.horizontal}>
                    <ToolbarButton icon="Undo" tooltip="Undo" />
                    <ToolbarButton icon="Redo" tooltip="Redo" />
                </Toolbar>

                <Toolbar direction={ToolbarDirection.horizontal} style={{ position: 'absolute', right: '0px', top: '0px' }}>
                    <ToolbarButton icon="Settings" tooltip="Settings" />
                    <ToolbarButton icon="PlayerSettings" tooltip="Profile" />
                    <ToolbarMenu icon="Feedback" tooltip="Feedback">
                        <ToolbarButton icon="Emoji2" tooltip="Like" />
                        <ToolbarButton icon="Sad" tooltip="Dislike" />
                        <ToolbarButton icon="Bug" tooltip="Register issue" />
                    </ToolbarMenu>
                </Toolbar>
            </div>
        </div>
    );
};


/*


// https://github.com/petercollingridge/code-for-blog/tree/master/svg-interaction
function makeDraggable(svg) {
    svg.addEventListener('mousedown', startDrag);
    svg.addEventListener('mousemove', drag);
    svg.addEventListener('mouseup', endDrag);
    svg.addEventListener('mouseleave', endDrag);
    svg.addEventListener('touchstart', startDrag);
    svg.addEventListener('touchmove', drag);
    svg.addEventListener('touchend', endDrag);
    svg.addEventListener('touchleave', endDrag);
    svg.addEventListener('touchcancel', endDrag);

    function getMousePosition(evt) {
        const CTM = svg.getScreenCTM();
        if (evt.touches) { evt = evt.touches[0]; }
        return {
            x: (evt.clientX - CTM.e) / CTM.a,
            y: (evt.clientY - CTM.f) / CTM.d
        };
    }

    let selectedElement, offset, transform;

    function bringToTopofSVG(targetElement) {
        const parent = targetElement.ownerSVGElement;
        parent.appendChild(targetElement);
    }

    function initializeDragging(evt) {
        offset = getMousePosition(evt);

        bringToTopofSVG(selectedElement);

        // Make sure the first transform on the element is a translate transform
        const transforms = selectedElement.transform.baseVal;

        if (transforms.length === 0 || transforms.getItem(0).type !== SVGTransform.SVG_TRANSFORM_TRANSLATE) {
            // Create an transform that translates by (0, 0)
            const translate = svg.createSVGTransform();
            translate.setTranslate(0, 0);
            selectedElement.transform.baseVal.insertItemBefore(translate, 0);
        }

        // Get initial translation
        transform = transforms.getItem(0);
        offset.x -= transform.matrix.e;
        offset.y -= transform.matrix.f;
    }

    function startDrag(evt) {
        if (evt.target.classList.contains('draggable')) {
            selectedElement = evt.target;
            initializeDragging(evt);
        } else if (evt.target.parentNode.classList.contains('draggable-group')) {
            selectedElement = evt.target.parentNode;
            initializeDragging(evt);
        }
    }

    function drag(evt) {
        if (selectedElement) {
            evt.preventDefault();
            const coord = getMousePosition(evt);
            transform.setTranslate(coord.x - offset.x, coord.y - offset.y);
        }
    }

    function endDrag(evt) {
        selectedElement = false;
    }
}

                <rect x="0" y="0" width="30" height="20" fill="#eee" />
                <text x="0" y="2.3" fontSize="3px" className="draggable">Hello world</text>
                <rect className="static" fill="#888" x="2" y="4" width="6" height="2" />
                <rect className="draggable" fill="#007bff" x="2" y="4" width="6" height="2" transform="rotate(90, 5, 5) translate(10, 0)" />
                <g className="draggable-group">
                    <ellipse fill="#ff00af" cx="5" cy="5" rx="3" ry="2" transform="translate(10, 0)" />
                    <polygon fill="#ffa500" transform="rotate(15, 15, 15)" points="16.9 15.6 17.4 18.2 15 17 12.6 18.2 13.1 15.6 11.2 13.8 13.8 13.4 15 11 16.2 13.4 18.8 13.8" />
                </g>
                <g className="draggable-group">
                    <path stroke="#2bad7b" strokeWidth="0.5" fill="none" d="M1 5C5 1 5 9 9 5" transform="translate(20)" />
                    <text x="25" y="15" textAnchor="middle" fontSize="3px" alignmentBaseline="middle">Drag</text>
                </g>

                <foreignObject x="0" y="0" width="800" height="600" className="draggable" transform="scale(0.0118 0.0118)">
                    <div className="content">
                        <PrimaryButton>Click me</PrimaryButton>
                    </div>
                </foreignObject>
*/
