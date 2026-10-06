import React, { useEffect } from 'react';
import { useAuth } from 'react-oidc-context';
import { useNavigate } from 'react-router-dom';

const AuthenticationCallback: React.FC = () => {
    const auth = useAuth();
    const navigate = useNavigate();

    useEffect(() => {
        if (!auth.isLoading && !auth.error) {
            navigate('/');
        }
    }, [auth.isLoading, auth.error, navigate]);

    if (auth.isLoading) {
        return <div>Loading...</div>;
    }

    if (auth.error) {
        return <div>Error: {auth.error.message}</div>;
    }

    return null;
};

export default AuthenticationCallback;