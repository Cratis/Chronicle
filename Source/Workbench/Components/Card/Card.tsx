// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type CSSProperties, type ReactNode } from 'react';
import { Card as PrimeCard } from 'primereact/card';

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
 * A declarative card over PrimeReact 11's compositional `Card` primitives.
 *
 * PrimeReact 10's `<Card header= footer=>` became a namespace of parts
 * (`Card.Root`, `Card.Header`, `Card.Body`, `Card.Footer`) in 11. The Workbench
 * uses the header/body/footer shape in a dozen places, so this keeps that one
 * authoring model rather than repeating the composition at every call site.
 */
export const Card = ({ header, footer, children, className, style }: CardProps) => (
    <PrimeCard.Root className={className} style={style}>
        {header && <PrimeCard.Header>{header}</PrimeCard.Header>}
        <PrimeCard.Body>{children}</PrimeCard.Body>
        {footer && <PrimeCard.Footer>{footer}</PrimeCard.Footer>}
    </PrimeCard.Root>
);
