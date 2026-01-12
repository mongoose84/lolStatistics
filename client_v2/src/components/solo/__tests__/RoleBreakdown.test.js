import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import RoleBreakdown from '../RoleBreakdown.vue'

describe('RoleBreakdown', () => {
  const mockRoles = [
    { role: 'MIDDLE', games: 50, winRate: 58, kda: 3.8, topChampions: [] },
    { role: 'JUNGLE', games: 30, winRate: 52, kda: 3.2, topChampions: [] },
    { role: 'TOP', games: 20, winRate: 45, kda: 2.8, topChampions: [] }
  ]

  it('renders card title', () => {
    const wrapper = mount(RoleBreakdown, {
      props: {
        roles: mockRoles
      }
    })

    expect(wrapper.find('.card-title').text()).toBe('Role Breakdown')
  })

  it('renders all role items sorted by games', () => {
    const wrapper = mount(RoleBreakdown, {
      props: {
        roles: mockRoles
      }
    })

    const roleItems = wrapper.findAll('.role-item')
    expect(roleItems).toHaveLength(3)
    
    // Should be sorted by games (descending)
    expect(roleItems[0].text()).toContain('Mid')
    expect(roleItems[1].text()).toContain('Jungle')
    expect(roleItems[2].text()).toContain('Top')
  })

  it('displays correct game counts', () => {
    const wrapper = mount(RoleBreakdown, {
      props: {
        roles: mockRoles
      }
    })

    expect(wrapper.text()).toContain('50 games')
    expect(wrapper.text()).toContain('30 games')
    expect(wrapper.text()).toContain('20 games')
  })

  it('displays correct winrates with classes', () => {
    const wrapper = mount(RoleBreakdown, {
      props: {
        roles: mockRoles
      }
    })

    expect(wrapper.find('.winrate-high').exists()).toBe(true) // 58%
    expect(wrapper.find('.winrate-good').exists()).toBe(true) // 52%
    expect(wrapper.find('.winrate-low').exists()).toBe(true)  // 45%
  })

  it('displays role icons', () => {
    const wrapper = mount(RoleBreakdown, {
      props: {
        roles: mockRoles
      }
    })

    expect(wrapper.text()).toContain('⚡') // Mid
    expect(wrapper.text()).toContain('🌲') // Jungle
    expect(wrapper.text()).toContain('🛡️') // Top
  })

  it('shows loading skeleton when loading', () => {
    const wrapper = mount(RoleBreakdown, {
      props: {
        roles: null,
        loading: true
      }
    })

    expect(wrapper.find('.loading-skeleton').exists()).toBe(true)
    expect(wrapper.findAll('.skeleton-role').length).toBe(5)
  })

  it('shows empty state when no roles', () => {
    const wrapper = mount(RoleBreakdown, {
      props: {
        roles: [],
        loading: false
      }
    })

    expect(wrapper.find('.empty-state').exists()).toBe(true)
    expect(wrapper.text()).toContain('No role data available')
  })

  it('renders champion mini icons when topChampions provided', () => {
    const rolesWithChamps = [
      { 
        role: 'MIDDLE', 
        games: 50, 
        winRate: 58, 
        kda: 3.8,
        topChampions: [
          { championId: 1, championName: 'Annie', games: 20, winRate: 60 },
          { championId: 2, championName: 'Brand', games: 15, winRate: 55 }
        ]
      }
    ]

    const wrapper = mount(RoleBreakdown, {
      props: {
        roles: rolesWithChamps
      }
    })

    expect(wrapper.findAll('.champion-mini').length).toBe(2)
  })
})

