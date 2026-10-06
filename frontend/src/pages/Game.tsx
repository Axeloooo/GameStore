import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { useBasket } from '../context/BasketContext';
import GamesClient from '../clients/GamesClient';
import { GameDetails } from '../models/GameDetails';
import { BasketItem } from '../models/BasketItem';
import { CommandResult } from '../models/CommandResult';

const Game: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const auth = useAuth();
    const { basket, addItem } = useBasket();
    const [game, setGame] = useState<GameDetails | null>(null);
    const [errorList, setErrorList] = useState<string[]>([]);

    useEffect(() => {
        const fetchGame = async () => {
            if (id) {
                try {
                    const gamesClient = new GamesClient();
                    const fetchedGame = await gamesClient.getGameAsync(id);
                    setGame(fetchedGame);
                    document.title = fetchedGame.name;
                } catch (error) {
                    if (error instanceof Error) {
                        setErrorList([error.message]);
                    } else {
                        setErrorList(['An unknown error occurred']);
                    }
                }
            }
        };

        fetchGame();
    }, [id]);

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();

        if (!auth.isAuthenticated) {
            auth.signinRedirect({ state: { from: window.location.pathname } });
            return;
        }

        if (!game) return;

        setErrorList([]);

        const newBasketItem: BasketItem = {
            id: game.id,
            name: game.name,
            price: game.price,
            quantity: 1,
            imageUri: game.imageUri || ''
        };

        try {
            const result: CommandResult = await addItem(newBasketItem);

            if (!result.succeeded) {
                setErrorList(result.errors);
            }
        } catch (error) {
            if (error instanceof Error) {
                setErrorList([error.message]);
            } else {
                setErrorList(['An unknown error occurred']);
            }
        }
    };

    return (
        <div>
            {!game ? (
                <p><em>Loading...</em></p>
            ) : (
                <>
                    {errorList.length > 0 && errorList.map((error, index) => (
                        <div key={index} className="alert alert-danger">{error}</div>
                    ))}

                    <div className="row mt-4">
                        <div className="col-md-4">
                            <img src={game.imageUri ?? ''} alt={game.name} className="img-fluid border border-secondary" />
                        </div>
                        <div className="col-md-5">
                            <h2>{game.name}</h2>
                            <p className="mt-3">{game.description}</p>
                            <p className="display-4 font-weight-bold">${game.price}</p>
                            {basket && basket.items.some(item => item.id === game.id) ? (
                                <a href="/cart" className="btn btn-primary">View in Cart</a>
                            ) : (
                                <form onSubmit={handleSubmit}>
                                    <button type="submit" className="btn btn-primary">Add to Cart</button>
                                </form>
                            )}
                            <p className="text-secondary mt-3">
                            Release Date: {new Date(game.releaseDate).toLocaleDateString('en-US', { month: 'short', day: '2-digit', year: 'numeric', timeZone: 'UTC' })}
                            </p>
                        </div>
                    </div>
                </>
            )}
        </div>
    );
};

export default Game;