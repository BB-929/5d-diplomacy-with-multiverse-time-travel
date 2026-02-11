using System.Data;
using Adjudication;
using Context;
using Entities;
using Enums;
using Exceptions;
using Factories;
using Microsoft.EntityFrameworkCore;

namespace Repositories;

public class WorldRepository(ILogger<WorldRepository> logger, GameContext context, RegionMapFactory regionMapFactory, DefaultWorldFactory defaultWorldFactory)
{
    private readonly ILogger<WorldRepository> logger = logger;
    private readonly GameContext context = context;
    private readonly RegionMapFactory regionMapFactory = regionMapFactory;
    private readonly DefaultWorldFactory defaultWorldFactory = defaultWorldFactory;

    public async Task<World> GetWorld(int gameId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var world = await GetWorldInternal(gameId, false);
        await transaction.CommitAsync();
        return world;
    }

    public async Task<int> GetIteration(int gameId)
    {
        logger.LogInformation("Querying iteration for game {GameId}", gameId);

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var iteration = await context.Worlds
            .Where(w => w.GameId == gameId)
            .Select(w => (int?)w.Iteration)
            .FirstOrDefaultAsync()
            ?? throw new GameNotFoundException();
        await transaction.CommitAsync();

        return iteration;
    }

    public async Task AddOrders(int gameId, Nation[] players, List<Order> orders)
    {
        logger.LogInformation("Submitting orders for game {GameId}", gameId);

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var game = await context.Games.FindAsync(gameId)
            ?? throw new GameNotFoundException();

        if (game.PlayersSubmitted.Intersect(players).Any())
        {
            logger.LogInformation("Found existing submission for players {Players}, ignoring new submission", players);
            return;
        }

        game.PlayersSubmitted = [.. game.PlayersSubmitted, .. players];

        var world = await GetWorldInternal(gameId, true);
        world.Orders.AddRange(orders);

        if (world.LivingPlayers.Count <= game.PlayersSubmitted.Count)
        {
            logger.LogInformation("Adjudicating game {GameId}", gameId);

            game.PlayersSubmitted = [];

            var adjudicator = new Adjudicator(world, game.HasStrictAdjacencies, regionMapFactory, defaultWorldFactory);
            adjudicator.Adjudicate();
            world.Iteration++;

            logger.LogInformation("Adjudicated game {GameId}", gameId);
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private async Task<World> GetWorldInternal(int gameId, bool shouldTrackEntities)
    {
        logger.LogInformation("Querying world for game {GameId}", gameId);

        private async Task AutoSubmitInactivePlayers(int gameId)
{
    logger.LogInformation("Auto-submitting orders for nations without players in game {GameId}", gameId);
    
    var game = await gameRepository.GetGame(gameId);
    
    // Skip for sandbox games (all nations have "players" in sandbox)
    if (game.IsSandbox)
        return;
    
    // Get all possible nations
    var allNations = Enum.GetValues<Nation>().ToList();
    
    // Get nations that have players assigned
    var nationsWithPlayers = game.Players;
    
    // Find nations without any player
    var nationsWithoutPlayers = allNations.Except(nationsWithPlayers).ToList();
    
    // Also exclude nations that already submitted
    var nationsToAutoSubmit = nationsWithoutPlayers.Except(game.PlayersSubmitted).ToList();
    
    if (nationsToAutoSubmit.Count == 0)
        return;
    
    logger.LogInformation("Auto-submitting for {Count} nations without players: {Nations}", 
        nationsToAutoSubmit.Count, string.Join(", ", nationsToAutoSubmit));
    
    // Submit empty orders (all units will hold) for nations without players
    await worldRepository.AddOrders(gameId, nationsToAutoSubmit.ToArray(), new List<Entities.Order>());
}

        // Should be called within a serializable transaction.
        var worldQuery = context.Worlds
            .Include(w => w.Boards).ThenInclude(b => b.Centres)
            .Include(w => w.Boards).ThenInclude(b => b.Units)
            .Include(w => w.Orders).ThenInclude(o => o.Unit)
            .AsSplitQuery();

        if (!shouldTrackEntities)
        {
            worldQuery = worldQuery.AsNoTracking();
        }

        var world = await worldQuery.FirstOrDefaultAsync(w => w.GameId == gameId)
            ?? throw new GameNotFoundException();

        return world;
    }
}
