// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from "react";
import * as PieTypes from './menu/PieTypes';
import { CircularMenu } from './menu/CircularMenu';

const menu = 0;

const menus = {
    CONTRAST: {
        data: [
            { label: '', icon: '', value: 20, bg: 'white', color: '#512DA8', type: 'ROOT' },
            { label: 'contrast', icon: '', value: 20, bg: '#9575CD', color: 'white', type: 'ROOT' },
            { label: 'battery', icon: '', value: 20, bg: '#7E57C2', color: 'white', type: 'ROOT' },
            { label: 'bluetooth', icon: '', value: 20, bg: '#673AB7', color: 'white', type: 'ROOT' },
            { label: 'light', icon: '', value: 20, bg: '#5E35B1', color: 'white', type: 'ROOT' },
            { label: 'settings', icon: '', value: 20, bg: '#512DA8', color: 'white', type: 'ROOT' },
            { label: 'contrast', icon: '', value: 20, bg: '#9575CD', color: 'white', type: 'ROOT' },
            { label: 'View', icon: '', value: 20, bg: '#7E57C2', color: 'white', type: 'ROOT' },
            { label: 'bluetooth', icon: '', value: 20, bg: '#673AB7', color: 'white', type: 'ROOT' },
            { label: 'light', icon: '', value: 20, bg: '#5E35B1', color: 'white', type: 'ROOT' }],
        config: {
            type: PieTypes.CIRCLE,
            colors: [],
            width: null,
            showIcon: true,
            sizeIcon: '1.6em',
            pieSize: 70
        }
    },

    BATTERY: {
        data: [{ label: '', icon: '', value: 20, bg: 'white', color: 'rgb(121, 121, 121)', type: 'ROOT' }, { label: 'contrast', icon: '', value: 20, bg: 'rgb(179, 179, 179)', color: '#222', type: 'ROOT' }, { label: 'battery', icon: '', value: 20, bg: 'rgb(140, 140, 140)', color: '#222', type: 'ROOT' }, { label: 'bluetooth', icon: '', value: 20, bg: 'rgb(133, 133, 133)', color: '#222', type: 'ROOT' }],
        config: {
            type: PieTypes.CIRCLE,
            colors: [],
            width: null,
            showIcon: true,
            sizeIcon: '1.2m',
            pieSize: 70
        }
    },

    LIGHTS: {
        data: [{ label: '', icon: '', value: 20, bg: 'white', color: 'rgb(193, 186, 33)', type: 'ROOT' }, { label: 'contrast', icon: '', value: 20, bg: 'rgb(238, 232, 85)', color: '#bdac18', type: 'ROOT' }, { label: 'battery', icon: '', value: 20, bg: 'rgb(246, 242, 130)', color: '#bdac18', type: 'ROOT' }, { label: 'bluetooth', icon: '', value: 20, bg: 'rgb(238, 232, 85)', color: '#bdac18', type: 'ROOT' }],
        config: {
            type: PieTypes.CIRCLE,
            colors: [],
            width: null,
            showIcon: true,
            sizeIcon: '1.6em',
            pieSize: 70
        }
    },

    SETTINGS: {
        data: [{ label: '', icon: '', value: 20, bg: 'white', color: 'rgb(121, 121, 121)', type: 'ROOT' }, { label: 'contrast', icon: '', value: 20, bg: 'rgb(179, 179, 179)', color: '#222', type: 'ROOT' }, { label: 'battery', icon: '', value: 20, bg: 'rgb(140, 140, 140)', color: '#222', type: 'ROOT' }, { label: 'bluetooth', icon: '', value: 20, bg: 'rgb(133, 133, 133)', color: '#222', type: 'ROOT' }],
        config: {
            type: PieTypes.CIRCLE,
            colors: [],
            width: null,
            showIcon: true,
            sizeIcon: '1.2m',
            pieSize: 70
        }
    },

    BLUETOOTH: {
        data: [
            { label: 'enable', value: 70, bg: 'rgb(57, 172, 221)', color: 'white', type: 'ROOT' },
            { label: 'back', value: 30, bg: 'white', color: 'rgb(57, 172, 221)', type: 'ROOT' }],
        config: {
            type: PieTypes.CIRCLE,
            colors: [],
            width: null,
            showIcon: false,
            sizeIcon: '1em',
            pieSize: 100
        }
    },

    ROOT: {
        data: [{ label: 'contrast', icon: '', value: 20, bg: '#9575CD', color: 'white', type: 'CONTRAST' }, { label: 'battery', icon: '', value: 20, bg: 'grey', color: 'white', type: 'BATTERY' }, { label: 'bluetooth', icon: '', value: 20, bg: 'rgb(142, 186, 223)', color: 'white', type: 'BLUETOOTH' }, { label: 'light', icon: '', value: 20, bg: 'rgb(242, 224, 131)', color: 'white', type: 'LIGHTS' }, { label: 'settings', icon: '', value: 20, bg: 'rgb(201, 143, 110)', color: 'white', type: 'SETTINGS' }],
        config: {
            type: PieTypes.CIRCLE,
            colors: [],
            width: null,
            showIcon: true,
            sizeIcon: '1.2em',
            pieSize: 50,
            showCenteredLabel: false
        }
    }
};

export const Menu = () => {
    const [menu, setMenu] = useState(menus.ROOT);
    const [visible, setVisible] = useState(false);

    return (
        <>
            <div onClick={() => {
                setVisible(true);
            }}>
                <CircularMenu
                    show={visible}
                    config={menu.config as any}
                    data={menu.data}
                    onItemClick={(d) => {
                        setMenu(menus[d.type]);
                    }}
                />
            </div>
        </>
    );
};
