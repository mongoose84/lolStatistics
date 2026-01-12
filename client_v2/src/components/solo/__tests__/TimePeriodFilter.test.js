import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import TimePeriodFilter from '../TimePeriodFilter.vue'

describe('TimePeriodFilter', () => {
  it('renders all time period options', () => {
    const wrapper = mount(TimePeriodFilter, {
      props: {
        modelValue: 'month'
      }
    })

    const select = wrapper.find('.time-select')
    const options = select.findAll('option')
    
    expect(options).toHaveLength(4)
    expect(options[0].text()).toBe('Last 7 Days')
    expect(options[1].text()).toBe('Last 30 Days')
    expect(options[2].text()).toBe('Last 3 Months')
    expect(options[3].text()).toBe('Last 6 Months')
  })

  it('has correct values for options', () => {
    const wrapper = mount(TimePeriodFilter, {
      props: {
        modelValue: 'month'
      }
    })

    const options = wrapper.findAll('option')
    
    expect(options[0].element.value).toBe('week')
    expect(options[1].element.value).toBe('month')
    expect(options[2].element.value).toBe('3months')
    expect(options[3].element.value).toBe('6months')
  })

  it('emits update:modelValue when selection changes', async () => {
    const wrapper = mount(TimePeriodFilter, {
      props: {
        modelValue: 'month'
      }
    })

    const select = wrapper.find('.time-select')
    await select.setValue('3months')
    
    expect(wrapper.emitted('update:modelValue')).toBeTruthy()
    expect(wrapper.emitted('update:modelValue')[0]).toEqual(['3months'])
  })

  it('defaults to month when no modelValue provided', () => {
    const wrapper = mount(TimePeriodFilter)
    
    expect(wrapper.props('modelValue')).toBe('month')
  })
})

