import axios from 'axios';

/**
 * Fetch best team champion combinations for a user.
 * @param {number|string} userId - The user ID.
 * @returns {Promise<Object>} The team champion combos data.
 */
export default async function getTeamChampionCombos(userId) {
  const response = await axios.get(`/api/v1.0/team-champion-combos/${userId}`);
  return response.data;
}

