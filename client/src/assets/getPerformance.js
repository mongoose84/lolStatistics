import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetches performance timeline data for charts.
 * @param {string|number} userId - The user ID
 * @param {number} limit - Number of games (20, 50, or 100)
 * @returns {Promise<{gamers: Array<{gamerName: string, dataPoints: Array}>}>}
 */
export default async function getPerformance(userId, limit = 100) {
	const base = getBaseApi();
	const { data } = await axios.get(`${base}/performance/${userId}`, {
		params: { limit }
	});
	return data;
}
