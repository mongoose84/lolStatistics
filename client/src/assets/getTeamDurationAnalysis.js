import axios from 'axios';

/**
 * Fetch team game duration analysis data for a user.
 * @param {number|string} userId - The user ID.
 * @returns {Promise<Object>} The team duration analysis data.
 */
export default async function getTeamDurationAnalysis(userId) {
  const response = await axios.get(`/api/v1.0/team-duration-analysis/${userId}`);
  return response.data;
}

