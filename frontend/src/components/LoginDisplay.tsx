import React from 'react';
import { useAuth } from 'react-oidc-context';
import sha256 from 'crypto-js/sha256';
import { encode } from 'utf8';

const LoginDisplay: React.FC = () => {
    const auth = useAuth();

    const getImageUrl = (email: string | null | undefined): string | undefined => {
        if (!email) {
            return undefined;
        }
        return `https://www.gravatar.com/avatar/${computeSha256Hash(email)}?d=retro`;
    };

    const computeSha256Hash = (rawData: string): string => {
        return sha256(encode(rawData)).toString();
    };

    const handleLogout = (event: React.FormEvent) => {
        event.preventDefault();
        auth.signoutRedirect();
    };

    return (
        <div>
            {auth.isAuthenticated ? (
                <div className="dropdown">
                    <a
                        className="link-body-emphasis text-decoration-none dropdown-toggle"
                        href="#"
                        id="dropdownMenuLink"
                        data-bs-toggle="dropdown"
                        aria-expanded="false"
                    >
                        <img
                            src={auth.user ? getImageUrl(auth.user.profile.email) : undefined}
                            alt="Your account"
                            width="32"
                            height="32"
                            className="rounded-circle"
                        />
                    </a>
                    <ul
                        className="dropdown-menu dropdown-menu-end"
                        aria-labelledby="dropdownMenuLink"
                    >
                        <li>
                            <a className="dropdown-item" href="#">
                                {auth.user ? auth.user.profile.name : 'User'}
                            </a>
                        </li>
                        <li>
                            <hr className="dropdown-divider" />
                        </li>
                        <li>
                            <form onSubmit={handleLogout}>
                                <button type="submit" className="dropdown-item">
                                    Log out
                                </button>
                            </form>
                        </li>
                    </ul>
                </div>
            ) : (
                <button type="button" onClick={() => auth.signinRedirect()} className="btn btn-primary">
                    Log in
                </button>
            )}
        </div>
    );
};

export default LoginDisplay;