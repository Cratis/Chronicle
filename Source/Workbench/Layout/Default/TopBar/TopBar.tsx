// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useLayoutContext } from '../context/LayoutContext';
import { Button } from 'primereact/button';
import css from './TopBar.module.css';
import { FaBars } from 'react-icons/fa6';
import { forwardRef } from 'react';
import { Logo } from "./Logo";
import { Profile } from "./Profile";
import { Notifications } from './Notifications';
import { Connection } from './Connection';

import { tw } from 'typewind';

export interface AppTopBarRef {
    menubutton?: HTMLButtonElement | null;
    topbarmenu?: HTMLDivElement | null;
    topbarmenubutton?: HTMLButtonElement | null;
}

export const TopBar = forwardRef<AppTopBarRef>(() => {
    const { toggleLeftSidebarOpen } = useLayoutContext();

    return (
        <div className={css.container}>
            <div className={`flex items-center justify-between ${css.leftSide}`}>
                <div className={css.sidebarToggle}>
                    <Button
                        onClick={toggleLeftSidebarOpen}
                        className={`p-button-rounded p-button-text ${tw.p_2}`}
                    >
                        <FaBars/>
                    </Button>
                </div>
                <div className={tw.flex_1.flex.align_middle.justify_center}>
                    <Logo/>
                </div>
            </div>
            <div className={tw.flex_1.flex.items_center.justify_end.px_5.gap_6}>
                <div>
                    <Connection/>
                </div>
                <div>
                    <Notifications/>
                </div>
                <div>
                    <Profile/>
                </div>
            </div>
        </div>
    );
});
