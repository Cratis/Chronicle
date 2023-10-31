import { Layout } from '../../Layout/Layout';

export interface IOpensjon {
    val?: string;
}

export function Opensjon(props: IOpensjon) {
    const { val } = props;

    return <Layout  >Opensjon {val}</Layout>;
}
