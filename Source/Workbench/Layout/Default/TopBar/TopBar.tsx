// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useLayoutContext } from '../context/LayoutContext';
import { Button } from 'primereact/button';
import css from './Topbar.module.css';
import { FaBars } from 'react-icons/fa6';
import { forwardRef } from 'react';
import { Logo } from "./Logo";
import { Profile } from "./Profile";
import { Notifications } from './Notifications';
import { Connection } from './Connection';

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
                        className='p-button-rounded p-button-text p-2'
                    >
                        <FaBars/>
                    </Button>
                </div>
                <div className={'flex-1 flex align-center justify-center'}>
                    <Logo/>
                </div>
            </div>
            <div className="flex-1 flex items-center justify-end px-5 gap-6">
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
