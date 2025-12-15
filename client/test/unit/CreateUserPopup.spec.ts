import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import CreateUserPopup from '@/views/CreateUserPopup.vue'

describe('CreateUserPopup', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.clearAllMocks()
  })

  it('submits username and accounts via onCreate and calls onClose', async () => {
    const onClose = vi.fn()
    const onCreate = vi.fn()

    const wrapper = mount(CreateUserPopup, {
      props: { onClose, onCreate }
    })

    // Fill top-level username
    const usernameInput = wrapper.findAll('input.username-input')[0]
    await usernameInput.setValue('Rasmus')

    // Fill the first summoner row (defaults exist with id=1)
    const gameInputs = wrapper.findAll('input.search-input')
    expect(gameInputs.length).toBeGreaterThan(0)
    await gameInputs[0].setValue('Aleno16')

    // Click Create
    const createBtn = wrapper.findAll('.action-buttons button')[0]
    await createBtn.trigger('click')

    // onCreate gets called with structured payload
    expect(onCreate).toHaveBeenCalledWith({
      username: 'Rasmus',
      accounts: [{ gameName: 'Aleno16', tagLine: 'EUNE' }]
    })

    // onClose called immediately after success
    expect(onClose).toHaveBeenCalled()
  })

  it('shows validation error when username is missing', async () => {
    const onClose = vi.fn()
    const onCreate = vi.fn()

    const wrapper = mount(CreateUserPopup, {
      props: { onClose, onCreate }
    })

    // Leave username empty, set a valid account
    const gameInputs = wrapper.findAll('input.search-input')
    await gameInputs[0].setValue('SomeName')

    const createBtn = wrapper.findAll('.action-buttons button')[0]
    await createBtn.trigger('click')

    expect(onCreate).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Please enter a username.')
    expect(onClose).not.toHaveBeenCalled()
  })

  it('shows validation error when no valid accounts provided', async () => {
    const onClose = vi.fn()
    const onCreate = vi.fn()

    const wrapper = mount(CreateUserPopup, {
      props: { onClose, onCreate }
    })

    // Set username but leave game name empty
    const usernameInput = wrapper.findAll('input.username-input')[0]
    await usernameInput.setValue('Rasmus')

    const createBtn = wrapper.findAll('.action-buttons button')[0]
    await createBtn.trigger('click')

    expect(onCreate).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Please enter at least one valid game name and tag.')
    expect(onClose).not.toHaveBeenCalled()
  })
})
