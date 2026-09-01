// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Popover, type PopoverRootOpenChangeEvent } from 'primereact/popover';
import * as icons from "react-icons/fa";
import css from './Profile.module.css';
import { Button } from '@cratis/components/Common';
import { useDarkMode } from 'usehooks-ts';
import strings from 'Strings';
import { useAuth } from '../../../Features/Security/AuthContext';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const ProfileItem = ({ icon, label, onClick }: { icon: any, label: string, onClick?: () => void }) => {
    return (
        <li className={css.profileItem} onClick={onClick}>
            <span className='mr-4'>{icon}</span>
            <span>{label}</span>
        </li>
    );
};

export const Profile = () => {
    const { isDarkMode, toggle: toggleDarkMode } = useDarkMode();
    const { logout } = useAuth();
    const [isProfilePanelOpen, setIsProfilePanelOpen] = useState(false);

    const handleLogout = async () => {
        setIsProfilePanelOpen(false);
        await logout();
    };

    return (
        <div className='flex-1'>
            <div className={'flex justify-end gap-3 '}>

                <Popover.Root
                    open={isProfilePanelOpen}
                    onOpenChange={(event: PopoverRootOpenChangeEvent) => setIsProfilePanelOpen(event.value ?? false)}>
                    <Popover.Trigger as="span">
                        <Button
                            icon={<icons.FaUser />}
                            shape='pill'
                            tone="accent"
                            className="p-2"
                            aria-label="User" />
                    </Popover.Trigger>

                    <Popover.Portal>
                        <Popover.Positioner>
                            <Popover.Popup className={css.overlayPanel}>
                                <Popover.Content>
                                    <ul className={css.profileItems}>
                                        <ProfileItem icon={<icons.FaUser />} label={strings.layout.topBar.profile.myAccount} />
                                        {isDarkMode ?
                                            <ProfileItem icon={<icons.FaSun />} label={strings.layout.topBar.profile.lightMode} onClick={toggleDarkMode} /> :
                                            <ProfileItem icon={<icons.FaMoon />} label={strings.layout.topBar.profile.darkMode} onClick={toggleDarkMode} />}
                                        <ProfileItem icon={<icons.FaSignOutAlt />} label="Logout" onClick={handleLogout} />
                                    </ul>
                                </Popover.Content>
                            </Popover.Popup>
                        </Popover.Positioner>
                    </Popover.Portal>
                </Popover.Root>
            </div>
        </div>);
};
