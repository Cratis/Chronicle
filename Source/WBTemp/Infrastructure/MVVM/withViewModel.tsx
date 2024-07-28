// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { container } from "tsyringe";
import { Constructor } from 'Infrastructure';
import { FunctionComponent, ReactElement, useState } from 'react';
import { Observer } from 'mobx-react';
import { makeAutoObservable } from 'mobx';
import { useParams } from 'react-router-dom';

export interface IViewContext<T, TProps = any> {
    viewModel: T,
    props: TProps,
}

export function withViewModel<TViewModel extends {}, TProps extends {} = {}>(viewModelType: Constructor<TViewModel>, targetComponent: FunctionComponent<IViewContext<TViewModel, TProps>>) {
    const renderComponent = (props: TProps) => {
        const [viewModel, setViewModel] = useState<TViewModel>();
        let vm = viewModel;

        if (!vm) {
            const child = container.createChildContainer();
            const params = useParams();
            child.registerInstance('props', props);
            child.registerInstance('params', params);
            vm = child.resolve<TViewModel>(viewModelType) as any;
            setViewModel(vm);
            makeAutoObservable(vm as any);
        }

        const component = () => targetComponent({ viewModel: vm!, props }) as ReactElement<any, string>;
        return <Observer>{component}</Observer>;
    };

    return renderComponent;
}
