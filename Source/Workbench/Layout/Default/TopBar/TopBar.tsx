// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useLayoutContext } from '../context/LayoutContext';
import { Button } from 'primereact/button';
import css from './TopBar.module.css';
import { FaBars } from 'react-icons/fa6';
import { Profile } from "./Profile";
import { Notifications } from './Notifications';
import { Connection } from './Connection';
import { useParams } from 'react-router-dom';
import { EventStore } from './EventStore';

type Params = {
    eventStoreId: string;
};


export const TopBar = () => {
    const params = useParams() as Params;

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
                <div className="flex-1 flex align-center justify-center">
                    <div className="font-extrabold text-2xl m-2">{params.eventStoreId}</div>
                </div>

            </div>
            <div className="flex-1 flex items-center justify-end px-5 gap-6">
                <div>
                    <EventStore />
                </div>
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
