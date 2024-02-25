// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useLayoutContext } from '../context/LayoutContext';
import { Button } from 'primereact/button';
import css from './TopBar.module.css';
import { FaBars, FaDatabase } from 'react-icons/fa6';
import { MdKeyboardArrowDown } from "react-icons/md";
import { Profile } from "./Profile";
import { Notifications } from './Notifications';
import { Connection } from './Connection';
import { useParams } from 'react-router-dom';
import { OverlayPanel } from 'primereact/overlaypanel';
import { useRef } from 'react';

type Params = {
    eventStoreId: string;
};


export const TopBar = () => {
    const params = useParams() as Params;
    const selectEventStorePanel = useRef<OverlayPanel>(null);
    const { toggleLeftSidebarOpen } = useLayoutContext();

    return (
        <div className={css.container}>
            <div className={`flex items-center justify-between ${css.leftSide}`}>
                <div className={css.sidebarToggle}>
                    <Button
                        onClick={toggleLeftSidebarOpen}
                        text
                        rounded
                        className='p-2'>
                        <FaBars />
                    </Button>
                </div>
                <div className={'flex-1 flex align-center justify-center'}>
                    <div className="flex mx-24 h-16 justify-evenly items-center cursor-pointer" onClick={(e) => selectEventStorePanel.current?.toggle(e)}>

                        <FaDatabase className='text-2xl' />
                        <div className='font-extrabold text-2xl m-2'>{params.eventStoreId}</div>
                        <MdKeyboardArrowDown className='text-2xl' />
                    </div>
                </div>

                <OverlayPanel ref={selectEventStorePanel}>
                </OverlayPanel>
            </div>
            <div className="flex-1 flex items-center justify-end px-5 gap-6">
                <div>
                    <Connection />
                </div>
                <div>
                    <Notifications />
                </div>
                <div>
                    <Profile />
                </div>
            </div>
        </div>
    );
};
