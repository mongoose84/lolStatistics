import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getMatchDuration(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/match-duration/${userId}`);
  return data;
}

