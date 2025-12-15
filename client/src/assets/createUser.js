import axios from 'axios';
import { getBaseApi } from './getHost';

var development = true;

// POST /api/v1.0/user/{username} with JSON body { accounts: [{ gameName, tagLine }, ...] }
export default async function createUser(username, accounts) {
  var base = getBaseApi();

  try {
    const response = await axios.post(
      `${base}/user/${encodeURIComponent(username)}`,
      { accounts },
      { headers: { 'Content-Type': 'application/json' } }
    );
    return response.data;
  } catch (e) {
    const errorMsg = e?.response?.data?.error || e.message || 'Request failed';
    throw new Error(errorMsg);
  }
}