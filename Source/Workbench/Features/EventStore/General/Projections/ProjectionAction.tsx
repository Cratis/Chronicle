// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useId, type ReactNode } from 'react';
import { Button, Tooltip } from '@cratis/components/Common';

interface ProjectionActionProps {
    label?: string;
    icon?: ReactNode;
    disabledReason?: string;
    onClick?: () => void;
}

/** An action whose disabled explanation is reachable by hover and keyboard focus. */
export const ProjectionAction = ({ label, icon, disabledReason, onClick }: ProjectionActionProps) => {
    const descriptionId = useId();

    if (!disabledReason) {
        return <Button variant='ghost' icon={icon} label={label} onClick={onClick} />;
    }

    return (
        <Tooltip content={disabledReason} position='bottom'>
            <span
                role='button'
                tabIndex={0}
                aria-disabled='true'
                aria-label={label}
                aria-describedby={descriptionId}
                style={{ display: 'inline-flex' }}>
                <Button
                    variant='ghost'
                    disabled
                    tabIndex={-1}
                    aria-hidden='true'
                    icon={icon}
                    label={label}
                    style={{ pointerEvents: 'none' }} />
                <span id={descriptionId} className='sr-only'>{disabledReason}</span>
            </span>
        </Tooltip>
    );
};
