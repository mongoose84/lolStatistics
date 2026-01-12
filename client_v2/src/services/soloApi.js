/**
 * Solo Dashboard API service for v2 endpoints
 * All endpoints use cookie-based session authentication
 */

import { getBaseApi } from './apiConfig'

const API_BASE = getBaseApi()

/**
 * Get solo dashboard data for a specific account
 * @param {string} puuid - The PUUID of the Riot account
 * @param {Object} options - Query options
 * @param {string} options.queueFilter - Queue filter: 'all_ranked', 'ranked_solo', 'ranked_flex', 'normal', 'aram'
 * @param {string} options.timePeriod - Time period: 'week', 'month', '3months', '6months'
 * @returns {Promise<Object>} Solo dashboard data
 */
export async function getSoloDashboard(puuid, { queueFilter = 'all_ranked', timePeriod = 'month' } = {}) {
  const params = new URLSearchParams()
  if (queueFilter) params.set('queueFilter', queueFilter)
  if (timePeriod) params.set('timePeriod', timePeriod)

  const url = `${API_BASE}/solo/${puuid}/dashboard?${params.toString()}`
  
  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to get solo dashboard')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

/**
 * Get champion matchups data for a specific account
 * @param {string} puuid - The PUUID of the Riot account
 * @param {Object} options - Query options
 * @param {string} options.queueFilter - Queue filter: 'all_ranked', 'ranked_solo', 'ranked_flex', 'normal', 'aram'
 * @returns {Promise<Object>} Champion matchups data
 */
export async function getChampionMatchups(puuid, { queueFilter = 'all_ranked' } = {}) {
  const params = new URLSearchParams()
  if (queueFilter) params.set('queueFilter', queueFilter)

  const url = `${API_BASE}/solo/${puuid}/matchups?${params.toString()}`

  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to get champion matchups')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

/**
 * Get performance trends data over time
 * @param {string} puuid - The PUUID of the Riot account
 * @param {Object} options - Query options
 * @param {string} options.queueFilter - Queue filter
 * @param {string} options.timePeriod - Time period for trend data
 * @returns {Promise<Object>} Performance trends (winrate, KDA over time)
 */
export async function getPerformanceTrends(puuid, { queueFilter = 'all_ranked', timePeriod = 'month' } = {}) {
  const params = new URLSearchParams()
  if (queueFilter) params.set('queueFilter', queueFilter)
  if (timePeriod) params.set('timePeriod', timePeriod)

  const url = `${API_BASE}/solo/${puuid}/trends?${params.toString()}`

  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to get performance trends')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

/**
 * Get role breakdown with main champions per role
 * @param {string} puuid - The PUUID of the Riot account
 * @param {Object} options - Query options
 * @param {string} options.queueFilter - Queue filter
 * @param {string} options.timePeriod - Time period
 * @returns {Promise<Object>} Role breakdown data with top 3 champions per role
 */
export async function getRoleBreakdown(puuid, { queueFilter = 'all_ranked', timePeriod = '6months' } = {}) {
  const params = new URLSearchParams()
  if (queueFilter) params.set('queueFilter', queueFilter)
  if (timePeriod) params.set('timePeriod', timePeriod)

  const url = `${API_BASE}/solo/${puuid}/roles?${params.toString()}`

  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to get role breakdown')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

// Queue filter constants
export const QUEUE_FILTERS = {
  ALL_RANKED: 'all_ranked',
  RANKED_SOLO: 'ranked_solo',
  RANKED_FLEX: 'ranked_flex',
  NORMAL: 'normal',
  ARAM: 'aram'
}

// Time period constants
export const TIME_PERIODS = {
  WEEK: 'week',
  MONTH: 'month',
  THREE_MONTHS: '3months',
  SIX_MONTHS: '6months'
}

