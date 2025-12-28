import axios from 'axios';

/**
 * Fetch team win rate trend data for a user.
 * @param {number|string} userId - The user ID.
 * @returns {Promise<Object>} The team win rate trend data.
 */
export default async function getTeamWinRateTrend(userId) {
  const response = await axios.get(`/api/v1.0/team-win-rate-trend/${userId}`);
  return response.data;
}

