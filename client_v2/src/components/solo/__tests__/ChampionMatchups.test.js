import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ChampionMatchups from '../ChampionMatchups.vue'

describe('ChampionMatchups', () => {
  const mockMatchups = [
    { championId: 1, championName: 'Annie', games: 30, winRate: 65, kda: 4.2, csPerMin: 7.5 },
    { championId: 2, championName: 'Brand', games: 25, winRate: 52, kda: 3.1, csPerMin: 6.8 },
    { championId: 3, championName: 'Caitlyn', games: 20, winRate: 45, kda: 2.8, csPerMin: 8.2 }
  ]

  it('renders card title', () => {
    const wrapper = mount(ChampionMatchups, {
      props: {
        matchups: mockMatchups
      }
    })

    expect(wrapper.find('.card-title').text()).toBe('Champion Performance')
  })

  it('displays matchup count', () => {
    const wrapper = mount(ChampionMatchups, {
      props: {
        matchups: mockMatchups
      }
    })

    expect(wrapper.find('.matchup-count').text()).toBe('3 champions')
  })

  it('renders table header', () => {
    const wrapper = mount(ChampionMatchups, {
      props: {
        matchups: mockMatchups
      }
    })

    expect(wrapper.find('.matchups-header').text()).toContain('Champion')
    expect(wrapper.find('.matchups-header').text()).toContain('Games')
    expect(wrapper.find('.matchups-header').text()).toContain('Win Rate')
    expect(wrapper.find('.matchups-header').text()).toContain('KDA')
  })

  it('renders all matchup rows', () => {
    const wrapper = mount(ChampionMatchups, {
      props: {
        matchups: mockMatchups
      }
    })

    const rows = wrapper.findAll('.matchup-row')
    expect(rows).toHaveLength(3)
  })

  it('displays champion names', () => {
    const wrapper = mount(ChampionMatchups, {
      props: {
        matchups: mockMatchups
      }
    })

    expect(wrapper.text()).toContain('Annie')
    expect(wrapper.text()).toContain('Brand')
    expect(wrapper.text()).toContain('Caitlyn')
  })

  it('displays game counts', () => {
    const wrapper = mount(ChampionMatchups, {
      props: {
        matchups: mockMatchups
      }
    })

    expect(wrapper.text()).toContain('30')
    expect(wrapper.text()).toContain('25')
    expect(wrapper.text()).toContain('20')
  })

  it('applies correct winrate classes', () => {
    const wrapper = mount(ChampionMatchups, {
      props: {
        matchups: mockMatchups
      }
    })

    expect(wrapper.find('.winrate-high').exists()).toBe(true) // 65%
    expect(wrapper.find('.winrate-good').exists()).toBe(true) // 52%
    expect(wrapper.find('.winrate-low').exists()).toBe(true)  // 45%
  })

  it('shows loading skeleton when loading', () => {
    const wrapper = mount(ChampionMatchups, {
      props: {
        matchups: null,
        loading: true
      }
    })

    expect(wrapper.find('.loading-skeleton').exists()).toBe(true)
    expect(wrapper.findAll('.skeleton-row').length).toBe(5)
  })

  it('shows empty state when no matchups', () => {
    const wrapper = mount(ChampionMatchups, {
      props: {
        matchups: [],
        loading: false
      }
    })

    expect(wrapper.find('.empty-state').exists()).toBe(true)
    expect(wrapper.text()).toContain('No champion data available')
  })

  it('shows "Show More" button when more than 10 matchups', () => {
    const manyMatchups = Array.from({ length: 15 }, (_, i) => ({
      championId: i,
      championName: `Champion${i}`,
      games: 20 - i,
      winRate: 50,
      kda: 3.0,
      csPerMin: 7.0
    }))

    const wrapper = mount(ChampionMatchups, {
      props: {
        matchups: manyMatchups
      }
    })

    expect(wrapper.find('.btn-show-more').exists()).toBe(true)
    expect(wrapper.find('.btn-show-more').text()).toContain('5 more')
  })

  it('shows all matchups after clicking "Show More"', async () => {
    const manyMatchups = Array.from({ length: 15 }, (_, i) => ({
      championId: i,
      championName: `Champion${i}`,
      games: 20 - i,
      winRate: 50,
      kda: 3.0,
      csPerMin: 7.0
    }))

    const wrapper = mount(ChampionMatchups, {
      props: {
        matchups: manyMatchups
      }
    })

    expect(wrapper.findAll('.matchup-row')).toHaveLength(10)

    await wrapper.find('.btn-show-more').trigger('click')

    expect(wrapper.findAll('.matchup-row')).toHaveLength(15)
    expect(wrapper.find('.btn-show-more').exists()).toBe(false)
  })
})

