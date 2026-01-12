import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import StatsOverview from '../StatsOverview.vue'

describe('StatsOverview', () => {
  const mockStats = {
    wins: 85,
    losses: 65,
    avgKills: 8.5,
    avgDeaths: 4.2,
    avgAssists: 7.8,
    avgCsPerMin: 7.1,
    avgVisionScore: 32.5,
    avgDamage: 18500,
    avgGold: 12300
  }

  it('renders card title', () => {
    const wrapper = mount(StatsOverview, {
      props: {
        stats: mockStats
      }
    })

    expect(wrapper.find('.card-title').text()).toBe('Performance Stats')
  })

  it('displays win/loss record correctly', () => {
    const wrapper = mount(StatsOverview, {
      props: {
        stats: mockStats
      }
    })

    expect(wrapper.find('.wins').text()).toBe('85W')
    expect(wrapper.find('.losses').text()).toBe('65L')
  })

  it('formats average stats correctly', () => {
    const wrapper = mount(StatsOverview, {
      props: {
        stats: mockStats
      }
    })

    const text = wrapper.text()
    expect(text).toContain('8.5')
    expect(text).toContain('4.2')
    expect(text).toContain('7.8')
    expect(text).toContain('7.1')
  })

  it('formats large numbers with k suffix', () => {
    const wrapper = mount(StatsOverview, {
      props: {
        stats: mockStats
      }
    })

    const text = wrapper.text()
    expect(text).toContain('18.5k')
    expect(text).toContain('12.3k')
  })

  it('shows loading skeleton when loading', () => {
    const wrapper = mount(StatsOverview, {
      props: {
        stats: null,
        loading: true
      }
    })

    expect(wrapper.find('.loading-skeleton').exists()).toBe(true)
    expect(wrapper.findAll('.skeleton-row').length).toBe(5)
  })

  it('shows empty state when no stats', () => {
    const wrapper = mount(StatsOverview, {
      props: {
        stats: null,
        loading: false
      }
    })

    expect(wrapper.find('.empty-state').exists()).toBe(true)
    expect(wrapper.text()).toContain('No stats available')
  })

  it('shows trend indicator when trends provided', () => {
    const wrapper = mount(StatsOverview, {
      props: {
        stats: mockStats,
        trends: {
          winRateTrend: 5.2
        }
      }
    })

    expect(wrapper.find('.trend-section').exists()).toBe(true)
    expect(wrapper.find('.trend-up').exists()).toBe(true)
    expect(wrapper.text()).toContain('+5.2%')
  })

  it('shows down trend for negative values', () => {
    const wrapper = mount(StatsOverview, {
      props: {
        stats: mockStats,
        trends: {
          winRateTrend: -3.5
        }
      }
    })

    expect(wrapper.find('.trend-down').exists()).toBe(true)
    expect(wrapper.text()).toContain('-3.5%')
  })
})

