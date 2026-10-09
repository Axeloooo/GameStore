import { CommandResult } from '../models/CommandResult';
import { GameDetails } from '../models/GameDetails';
import { GamesPage } from '../models/GamesPage';
import { GameSummary } from '../models/GameSummary';

class GamesClient {
  private baseUrl = import.meta.env.VITE_BACKEND_API_URL;
  private accessToken: string | null;

  constructor(accessToken: string | null = null) {
    this.accessToken = accessToken;
  }  

  async getGamesAsync(pageNumber: number, pageSize: number, nameSearch?: string): Promise<GamesPage> {
    const url = new URL(`${this.baseUrl}/games`);
    url.searchParams.append('pageNumber', pageNumber.toString());
    url.searchParams.append('pageSize', pageSize.toString());
    if (nameSearch) {
      url.searchParams.append('name', nameSearch);
    }

    const response = await this.fetchWithHandling(url.toString());
    if (!response.ok) {
      const errorMessages = await this.handleFetchError(response);
      throw new Error(errorMessages.join('\n'));
    }

    const data = await response.json();

    // Transform releaseDate strings to Date objects and format as MM/dd/yyyy
    const transformedData = data.data.map((game: GameSummary) => {
      const date = new Date(game.releaseDate);
      const formattedDate = `${String(date.getUTCMonth() + 1).padStart(2, '0')}/${String(date.getUTCDate()).padStart(2, '0')}/${date.getUTCFullYear()}`;
      return {
        ...game,
        releaseDate: formattedDate,
      };
    });

    return {
      ...data,
      data: transformedData,
    };
  }

  async addGameAsync(game: GameDetails): Promise<CommandResult> {
    const response = await this.fetchWithHandling(`${this.baseUrl}/games`, {
      method: 'POST',
      body: this.toMultiPartFormDataContent(game),
    });

    if (!response.ok) {
      const errorMessages = await this.handleFetchError(response);
      return { succeeded: false, errors: errorMessages };
    }

    return { succeeded: true, errors: [] };
  }

  async getGameAsync(id: string): Promise<GameDetails> {
    const response = await this.fetchWithHandling(`${this.baseUrl}/games/${id}`);

    if (!response.ok) {
      const errorMessages = await this.handleFetchError(response);
      throw new Error(errorMessages.join('\n'));
    }

    return await response.json();
  }

  async updateGameAsync(updatedGame: GameDetails): Promise<CommandResult> {
    const response = await this.fetchWithHandling(`${this.baseUrl}/games/${updatedGame.id}`, {
      method: 'PUT',
      body: this.toMultiPartFormDataContent(updatedGame),
    });

    if (!response.ok) {
      const errorMessages = await this.handleFetchError(response);
      return { succeeded: false, errors: errorMessages };
    }

    return { succeeded: true, errors: [] };
  }

  async deleteGameAsync(id: string): Promise<CommandResult> {
    const response = await this.fetchWithHandling(`${this.baseUrl}/games/${id}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      const errorMessages = await this.handleFetchError(response);
      return { succeeded: false, errors: errorMessages };
    }

    return { succeeded: true, errors: [] };
  }

  private toMultiPartFormDataContent(game: GameDetails): FormData {
    const formData = new FormData();
    formData.append('name', game.name);
    if (game.genreId !== null) {
      formData.append('genreId', game.genreId);
    }
    formData.append('description', game.description);
    formData.append('price', game.price.toString());
    formData.append('releaseDate', game.releaseDate);
    if (game.imageFile) {
      formData.append('imageFile', game.imageFile);
    }
    return formData;
  }

  private async fetchWithHandling(url: string, options?: RequestInit): Promise<Response> {
    const headers = new Headers(options?.headers || {});

    if (this.accessToken) {
      headers.append('Authorization', `Bearer ${this.accessToken}`);
    }

    const updatedOptions: RequestInit = {
      ...options,
      headers,
    };

    try {
      const response = await fetch(url, updatedOptions);
      return response;
    } catch (error) {
      if (error instanceof TypeError) {
        throw new Error('We are currently experiencing issues loading the data. Please try again later.');
      }
      throw error;
    }
  }

  private async handleFetchError(response: Response): Promise<string[]> {
    let errorMessages: string[] = ['Unknown error'];
    try {
      const errorData = await response.json();
      if (errorData.title) {
        errorMessages = [errorData.title];
        if (errorData.errors && Array.isArray(errorData.errors)) {
          errorMessages = errorMessages.concat(errorData.errors);
        }
      } else if (errorData.errors && Array.isArray(errorData.errors)) {
        errorMessages = errorData.errors;
      } else if (errorData.detail) {
        errorMessages = [errorData.detail];
      }
    } catch (e) {
      console.error('Error parsing error response:', e);
    }
    return errorMessages;
  }
}

export default GamesClient;