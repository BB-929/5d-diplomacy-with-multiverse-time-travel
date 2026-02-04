const baseUrl = 'https://fived-diplomacy-with-multiverse-time-vy1t.onrender.com';

const routes = {
  createGame: () => `${baseUrl}/game`,
  joinGame: (gameId: number) => `${baseUrl}/game/${gameId}/players`,
  getWorld: (gameId: number) => `${baseUrl}/game/${gameId}`,
  submitOrders: (gameId: number) => `${baseUrl}/game/${gameId}/orders`,
  getIteration: (gameId: number) => `${baseUrl}/game/${gameId}/iteration`,
  getPlayersSubmitted: (gameId: number) => `${baseUrl}/game/${gameId}/players/submitted`,
};

export default routes;
