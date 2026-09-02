// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type CSSProperties, type ReactNode } from 'react';
import { Surface } from '@cratis/components/Common';
import './Card.css';

/**
 * Props for {@link Card}.
 */
export interface CardProps {
    /** Rendered above the body, inside the card's own header slot. */
    header?: ReactNode;
    /** Rendered below the body. */
    footer?: ReactNode;
    /** The card body. */
    children?: ReactNode;
    /** Applied to the card root. */
    className?: string;
    /** Applied to the card root. */
    style?: CSSProperties;
}

/**
 * A card with optional header and footer slots.
 *
 * Components 4 has no card of its own — `Surface` supplies the chrome (border, radius,
 * card background and subtle shadow) and this adds the header/body/footer split the
 * Workbench's dashboard widgets and event-store tiles are written against.
 */
export const Card = ({ header, footer, children, className, style }: CardProps) => (
    <Surface className={className ? `workbench-card ${className}` : 'workbench-card'} style={style}>
        {header && <div className='workbench-card__header'>{header}</div>}
        <div className='workbench-card__body'>{children}</div>
        {footer && <div className='workbench-card__footer'>{footer}</div>}
    </Surface>
);
