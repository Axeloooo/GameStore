import React, { useState, useEffect } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import GamesClient from '../../clients/GamesClient';
import { GameSummary } from '../../models/GameSummary';
import Pagination from '../../components/Pagination';
import { PaginationInfo } from '../../models/PaginationInfo';
import DeleteGameModal from '../../components/DeleteGameModal';
import { useAuth } from 'react-oidc-context';

// Declare bootstrap property on window object
declare global {
    interface Window {
        bootstrap: any;
    }
}

const Catalog: React.FC = () => {
    const [searchParams, setSearchParams] = useSearchParams();
    const [nameSearch, setNameSearch] = useState<string | null>(searchParams.get('name'));
    const [gamesPage, setGamesPage] = useState<{ data: GameSummary[] } | null>(null);
    const [paginationInfo, setPaginationInfo] = useState<PaginationInfo | null>(null);
    const [loadingErrorList, setLoadingErrorList] = useState<string[]>([]);
    const [errorList, setErrorList] = useState<string[]>([]);
    const [gameToDelete, setGameToDelete] = useState<GameSummary | null>(null);
    const auth = useAuth();  
    const pageSize = 5;

    const fetchGames = async () => {
        const pageNumber = parseInt(searchParams.get('page') || '1', 10);
        const name = searchParams.get('name') || '';
        try {
            const gamesClient = new GamesClient(auth.user?.access_token || null);
            const response = await gamesClient.getGamesAsync(pageNumber, pageSize, name);
            setGamesPage(response);
            setPaginationInfo(new PaginationInfo(pageNumber, response.totalPages, name));
        } catch (error: unknown) {
            if (error instanceof Error) {
                setLoadingErrorList([error.message]);
            } else {
                setLoadingErrorList(['An unknown error occurred']);
            }
        }
    };    

    useEffect(() => {
        document.title = 'Game Catalog';
        fetchGames();
    }, [searchParams, auth.user?.access_token]);

    useEffect(() => {
        if (gameToDelete) {
            const modal = new window.bootstrap.Modal(document.getElementById(`deleteModal-${gameToDelete.id}`)!);
            modal.show();
        }
    }, [gameToDelete]);

    const handleSearch = (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const params: any = {};
        if (nameSearch) {
            params.name = nameSearch;
            params.page = 1;
        } else {
            params.page = 1;
        }
        setSearchParams(params);
    };

    const handleInput = (event: React.ChangeEvent<HTMLInputElement>) => {
        const value = event.target.value;
        setNameSearch(value);
        if (value === '') {
            // Reload the games list when the search box is cleared
            setSearchParams({ page: '1' });
        }
    };

    const handleDelete = async (gameId: string) => {
        setErrorList([]);
        try {
            const gamesClient = new GamesClient(auth.user?.access_token || null);
            const result = await gamesClient.deleteGameAsync(gameId);

            if (result.succeeded) {
                fetchGames();
            } else {
                setErrorList(result.errors);
            }
        } catch (error: unknown) {
            if (error instanceof Error) {
                setErrorList([error.message]);
            } else {
                setErrorList(['An unknown error occurred']);
            }
        }
    };

    if (loadingErrorList.length > 0) {
        return <div>
            {loadingErrorList.map((error, index) => (
                <div key={index} className="mt-3 text-danger">
                    <em>{error}</em>
                </div>
            ))}
        </div>
    }

    if (gamesPage === null || paginationInfo === null) {
        return <p className="mt-3"><em>Loading...</em></p>;
    }

    return (
        <div>
            <div className="row mt-2">
                <div className="col">
                    <Link className="btn btn-primary" to="/catalog/editgame" role="button">
                        New Game
                    </Link>
                </div>
                <div className="col-sm-4">
                    <form id="searchGamesForm" method="post" className="d-flex" role="search" onSubmit={handleSearch}>
                        <input
                            className="form-control me-2"
                            type="search"
                            value={nameSearch || ''}
                            onChange={(e) => setNameSearch(e.target.value)}
                            onInput={handleInput}
                            placeholder="Search..."
                            aria-label="Search"
                        />
                        <button className="btn btn-outline-primary" type="submit">Search</button>
                    </form>
                </div>
            </div>


            {errorList.length > 0 && (
                <div className="modal-body mt-3">
                    {errorList.map((error, index) => (
                        <div key={index} className="alert alert-danger">
                            {error}
                        </div>
                    ))}
                </div>
            )}

            <table className="table table-striped table-bordered table-hover mt-3">
                <thead className="table-dark">
                    <tr>
                        <th>Image</th>
                        <th>Name</th>
                        <th>Genre</th>
                        <th className="text-end">Price</th>
                        <th>Release Date</th>
                        <th>Last Updated</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody>
                    {gamesPage.data.map((game) => (
                        <tr key={game.id}>
                            <td style={{ width: '60px', maxWidth: '60px' }}>
                                <img src={game.imageUri} alt={game.name} style={{ width: '50px', objectFit: 'contain' }} />
                            </td>
                            <td>{game.name}</td>
                            <td>{game.genre}</td>
                            <td className="text-end">${game.price}</td>
                            <td>{game.releaseDate}</td>
                            <td>{game.lastUpdatedBy}</td>
                            <td>
                                <div className="d-flex">
                                    <Link className="btn btn-primary me-2" to={`/catalog/editgame/${game.id}`} role="button">
                                        <i className="bi bi-pencil"></i>
                                    </Link>
                                    <button className="btn btn-danger" onClick={() => setGameToDelete(game)}>
                                        <i className="bi bi-x-lg"></i>
                                    </button>
                                </div>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>

            <div className="row mt-2">
                <div className="col">
                    <Pagination paginationInfo={paginationInfo} onPageChange={(pageNumber) => setSearchParams({ page: pageNumber.toString(), name: nameSearch || '' })} />
                </div>
            </div>

            {/* Delete Confirmation Modal */}
            {gameToDelete && (
                <DeleteGameModal game={gameToDelete} onDelete={handleDelete} />
            )}
        </div>
    );
};

export default Catalog;