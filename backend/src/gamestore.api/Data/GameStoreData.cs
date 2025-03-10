// using gamestore.api.Models;

// namespace gamestore.api.Data;

// public class GameStoreData
// {
//     private readonly List<Genre> genres =
//     [
//         new Genre { Id = new Guid("6b1de8b3-2b69-4f2c-9a4d-5b1e8a2f1f13"), Name = "Fighting" },
//         new Genre { Id = new Guid("659b63485-4a9f-4b6a-90b1-c42f4e74f8f1"), Name = "Roleplaying" },
//         new Genre { Id = new Guid("a12e45fa-bb9f-4f92-a5ad-4a6c9c5e8f2d"), Name = "Sports" },
//     ];

//     private readonly List<Game> games;

//     public GameStoreData()
//     {
//         games =
//         [
//             new Game
//             {
//                 Id = Guid.NewGuid(),
//                 Name = "Street Fighter II",
//                 Genre = genres[0],
//                 GenreId = genres[0].Id,
//                 Price = 19.99m,
//                 Description =
//                     "A revolutionary fighting game that popularized the genre with its iconic characters, special moves, and competitive gameplay. Challenge opponents in intense one-on-one battles and master each character's unique abilities.",
//                 ReleaseDate = new DateOnly(1992, 7, 15),
//             },
//             new Game
//             {
//                 Id = Guid.NewGuid(),
//                 Name = "Final Fantasy XIV",
//                 Genre = genres[1],
//                 GenreId = genres[1].Id,
//                 Price = 59.99m,
//                 Description =
//                     "An expansive MMORPG set in the world of Eorzea, offering rich storytelling, complex dungeons, and an engaging class system. Join millions of players worldwide to complete quests, defeat powerful enemies, and shape the fate of the realm.",
//                 ReleaseDate = new DateOnly(2010, 9, 30),
//             },
//             new Game
//             {
//                 Id = Guid.NewGuid(),
//                 Name = "FIFA 23",
//                 Genre = genres[2],
//                 GenreId = genres[2].Id,
//                 Price = 69.99m,
//                 Description =
//                     "The latest installment in the iconic football simulation series, featuring hyper-realistic gameplay, updated teams, and new modes. Experience the thrill of the pitch with improved AI, realistic ball control, and expanded career options.",
//                 ReleaseDate = new DateOnly(2023, 9, 27),
//             },
//         ];
//     }

//     public IEnumerable<Game> GetGames() => games;

//     public Game? GetGame(Guid id) => games.Find(game => game.Id == id);

//     public void AddGame(Game game)
//     {
//         game.Id = Guid.NewGuid();
//         games.Add(game);
//     }

//     public void RemoveGame(Guid id) => games.RemoveAll(game => game.Id == id);

//     public IEnumerable<Genre> GetGenres() => genres;

//     public Genre? GetGenre(Guid id) => genres.Find(genre => genre.Id == id);
// }
