import axios from 'axios';
import { getBaseApi } from './getHost.js';

// GET /api/v1.0/users => [{ UserId, UserName }, ...]
export default async function getGamers(userId) {
	const base = getBaseApi();
	const { data } = await axios.get(`${base}/gamers/${userId}`);
	return data;
}