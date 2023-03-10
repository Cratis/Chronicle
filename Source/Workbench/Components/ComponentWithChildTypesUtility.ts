// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReactElement, JSXElementConstructor } from 'react';


export interface PropsForComponentWithChildTypes {
    children?: ReactElement | ReactElement[];
}

export class ChildTypes {

    constructor(private actualChildren: ReactElement[]) {
    }

    static get(props: PropsForComponentWithChildTypes): ChildTypes {
        if (!props.children) {
            return new ChildTypes([]);
        }
        if (props.children instanceof Array) {
            return new ChildTypes([...props.children]);
        } else {
            return new ChildTypes([props.children]);
        }
    }

    getSpecificType(type: JSXElementConstructor<any>): ReactElement[] | null {
        const elements = this.actualChildren.filter(_ => _ && _.type === type);

        if (elements.length > 0) {
            return elements as ReactElement[];
        }

        return null;
    }

    getSingleSpecificType(type: JSXElementConstructor<any>): ReactElement | null {
        const elements = this.getSpecificType(type);

        if (elements && elements.length > 0) {
            return elements[0] as ReactElement;
        }

        return null;
    }
}




