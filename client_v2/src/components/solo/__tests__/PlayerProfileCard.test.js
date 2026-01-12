import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import PlayerProfileCard from '../PlayerProfileCard.vue'

describe('PlayerProfileCard', () => {
  const mockAccount = {
    puuid: 'test-puuid',
    gameName: 'TestPlayer',
    tagLine: 'EUW',
    region: 'euw1',
    profileIconId: 1234
  }

  const mockStats = {
    totalGames: 150,
    winRate: 55.5,
    kda: 3.45,
    avgCS: 7.2,
    rank: 'Gold II'
  }

  it('renders player name and tagline', () => {
    const wrapper = mount(PlayerProfileCard, {
      props: {
        account: mockAccount,
        stats: mockStats
      }
    })

    expect(wrapper.find('.player-name').text()).toContain('TestPlayer')
    expect(wrapper.find('.tag-line').text()).toBe('#EUW')
  })

  it('displays region badge', () => {
    const wrapper = mount(PlayerProfileCard, {
      props: {
        account: mockAccount,
        stats: mockStats
      }
    })

    expect(wrapper.find('.region-badge').text()).toBe('EUW')
  })

  it('displays rank badge when rank is provided', () => {
    const wrapper = mount(PlayerProfileCard, {
      props: {
        account: mockAccount,
        stats: mockStats
      }
    })

    expect(wrapper.find('.rank-badge').text()).toBe('Gold II')
  })

  it('displays quick stats correctly', () => {
    const wrapper = mount(PlayerProfileCard, {
      props: {
        account: mockAccount,
        stats: mockStats
      }
    })

    const statValues = wrapper.findAll('.stat-value')
    expect(statValues.length).toBeGreaterThanOrEqual(4)
    expect(wrapper.text()).toContain('150')
    expect(wrapper.text()).toContain('56%') // Rounded from 55.5
    expect(wrapper.text()).toContain('3.45')
  })

  it('shows placeholder when no profile icon', () => {
    const wrapper = mount(PlayerProfileCard, {
      props: {
        account: { ...mockAccount, profileIconId: null },
        stats: mockStats
      }
    })

    expect(wrapper.find('.icon-placeholder').exists()).toBe(true)
    expect(wrapper.find('.icon-placeholder').text()).toBe('T')
  })

  it('shows loading skeleton when loading', () => {
    const wrapper = mount(PlayerProfileCard, {
      props: {
        account: mockAccount,
        stats: null,
        loading: true
      }
    })

    expect(wrapper.find('.skeleton').exists()).toBe(true)
  })

  it('applies correct winrate class for high winrate', () => {
    const wrapper = mount(PlayerProfileCard, {
      props: {
        account: mockAccount,
        stats: { ...mockStats, winRate: 60 }
      }
    })

    expect(wrapper.find('.winrate-high').exists()).toBe(true)
  })

  it('applies correct winrate class for low winrate', () => {
    const wrapper = mount(PlayerProfileCard, {
      props: {
        account: mockAccount,
        stats: { ...mockStats, winRate: 45 }
      }
    })

    expect(wrapper.find('.winrate-low').exists()).toBe(true)
  })
})

