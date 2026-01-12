import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import QueueFilter from '../QueueFilter.vue'

describe('QueueFilter', () => {
  it('renders all queue options', () => {
    const wrapper = mount(QueueFilter, {
      props: {
        modelValue: 'all_ranked'
      }
    })

    const buttons = wrapper.findAll('.filter-btn')
    expect(buttons).toHaveLength(5)
    expect(buttons[0].text()).toBe('All Ranked')
    expect(buttons[1].text()).toBe('Solo/Duo')
    expect(buttons[2].text()).toBe('Flex')
    expect(buttons[3].text()).toBe('Normal')
    expect(buttons[4].text()).toBe('ARAM')
  })

  it('applies active class to selected option', () => {
    const wrapper = mount(QueueFilter, {
      props: {
        modelValue: 'ranked_solo'
      }
    })

    const buttons = wrapper.findAll('.filter-btn')
    expect(buttons[0].classes()).not.toContain('active')
    expect(buttons[1].classes()).toContain('active')
  })

  it('emits update:modelValue when button is clicked', async () => {
    const wrapper = mount(QueueFilter, {
      props: {
        modelValue: 'all_ranked'
      }
    })

    await wrapper.findAll('.filter-btn')[2].trigger('click')
    
    expect(wrapper.emitted('update:modelValue')).toBeTruthy()
    expect(wrapper.emitted('update:modelValue')[0]).toEqual(['ranked_flex'])
  })

  it('defaults to all_ranked when no modelValue provided', () => {
    const wrapper = mount(QueueFilter)
    
    expect(wrapper.props('modelValue')).toBe('all_ranked')
  })
})

