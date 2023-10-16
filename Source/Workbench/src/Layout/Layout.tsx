type LayoutProps = {
    children: React.ReactNode;
}

export const Layout = ({ children }: LayoutProps) => {
    return (
        <div>
            <h1>Layout</h1>
            {children}
        </div>
    );
}