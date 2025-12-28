import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetches role distribution data for all gamers of a user.
 * @param {string|number} userId - The user ID
 * @returns {Promise<{gamers: Array<{gamerName: string, serverName: string, roles: Array<{position: string, gamesPlayed: number, percentage: number}>}>}>}
 */
export default async function getRoleDistribution(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/role-distribution/${userId}`);
  return data;
}

