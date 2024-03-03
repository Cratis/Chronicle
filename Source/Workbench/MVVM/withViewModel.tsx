// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { container } from "tsyringe";
import { Constructor } from '@aksio/fundamentals';
import { FunctionComponent, ReactElement } from 'react';
import { Observer } from 'mobx-react';
import { makeAutoObservable } from 'mobx';

export interface IViewContext<T, TProps = any> {
    viewModel: T,
    props: TProps,
}

export function withViewModel<TViewModel extends {}, TProps = {}>(viewModelType: Constructor<TViewModel>, targetComponent: FunctionComponent<IViewContext<TViewModel, TProps>>) {
    let renderComponent = (props: TProps) => {
        const viewModel = container.resolve<TViewModel>(viewModelType) as any;

        for(let key in props) {
            viewModel[key] = props[key];
        }

        makeAutoObservable(viewModel as any);
        const component = () => targetComponent({ viewModel, props }) as ReactElement<any, string>;
        return <Observer>{component}</Observer>;
    };

    return renderComponent;
}
