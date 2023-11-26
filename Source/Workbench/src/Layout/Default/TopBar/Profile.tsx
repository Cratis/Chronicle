// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useRef } from 'react';
import { OverlayPanel } from 'primereact/overlaypanel';
import { FaUser } from "react-icons/fa";
import * as css from './Profile.module.css';
import { Button } from 'primereact/button';
import { ThemeSwitch } from './ThemeSwitch';


export const Profile = () => {
    const overlayPanelRef = useRef<OverlayPanel>(null);

    return (
        <div className='flex-1'>
            <div className={'flex justify-end gap-3 '}>

                <Button
                    icon={FaUser}
                    rounded
                    severity="info"
                    onClick={(e) => overlayPanelRef.current?.toggle(e)}
                    aria-label="User" />


                <OverlayPanel ref={overlayPanelRef} className={css.overlayPanel}>
                    <ThemeSwitch />
                </OverlayPanel>
            </div>
        </div>)
}
