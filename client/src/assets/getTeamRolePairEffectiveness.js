import axios from 'axios';

/**
 * Fetch team role pair effectiveness data for a user.
 * @param {number|string} userId - The user ID.
 * @returns {Promise<Object>} The role pair effectiveness data.
 */
export default async function getTeamRolePairEffectiveness(userId) {
  const response = await axios.get(`/api/v1.0/team-role-pair-effectiveness/${userId}`);
  return response.data;
}

