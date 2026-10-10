// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Button } from '@cratis/components/Common';
import { Popover, type PopoverRootOpenChangeEvent } from 'primereact/popover';
import { FaBookOpen, FaCircleQuestion, FaDiscord } from 'react-icons/fa6';
import strings from 'Strings';
import css from './Help.module.css';

export const Help = () => {
    const [isHelpPanelOpen, setIsHelpPanelOpen] = useState(false);

    return (
        <Popover.Root
            open={isHelpPanelOpen}
            onOpenChange={(event: PopoverRootOpenChangeEvent) => setIsHelpPanelOpen(event.value ?? false)}>
            <Popover.Trigger as="span">
                <Button icon={<FaCircleQuestion />} variant='ghost'>
                    {strings.layout.topBar.help.title}
                </Button>
            </Popover.Trigger>
            <Popover.Portal>
                <Popover.Positioner>
                    <Popover.Popup>
                        <Popover.Content>
                            <nav aria-label={strings.layout.topBar.help.title} className={css.links}>
                                <a href="https://www.cratis.io/chronicle/" target="_blank" rel="noopener noreferrer" className={css.link}>
                                    <FaBookOpen aria-hidden="true" />
                                    <span>{strings.layout.topBar.help.documentation}</span>
                                </a>
                                <a href="https://discord.gg/kt4AMpV8WV" target="_blank" rel="noopener noreferrer" className={css.link}>
                                    <FaDiscord aria-hidden="true" />
                                    <span>{strings.layout.topBar.help.discord}</span>
                                </a>
                                <p className={css.description}>{strings.layout.topBar.help.communityDescription}</p>
                            </nav>
                        </Popover.Content>
                    </Popover.Popup>
                </Popover.Positioner>
            </Popover.Portal>
        </Popover.Root>
    );
};
