import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import CreateUserPopup from '@/views/CreateUserPopup.vue'

vi.mock('@/assets/createUser.js', () => ({
  default: vi.fn().mockResolvedValue({ ok: true })
}))

import createUser from '@/assets/createUser.js'

describe('CreateUserPopup', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.clearAllMocks()
  })

  it('submits username and accounts, shows success, and calls onClose', async () => {
    const onClose = vi.fn()
    const onCreate = vi.fn()

    const wrapper = mount(CreateUserPopup, {
      props: { onClose, onCreate }
    })

    // Fill top-level username
    const usernameInput = wrapper.findAll('input.username-input')[0]
    await usernameInput.setValue('bob')

    // Fill the first summoner row (defaults exist with id=1)
    const gameInputs = wrapper.findAll('input.search-input')
    expect(gameInputs.length).toBeGreaterThan(0)
    await gameInputs[0].setValue('GamerOne')

    // Click Create
    const createBtn = wrapper.findAll('.action-buttons button')[0]
    await createBtn.trigger('click')

    // Ensure API called with mapped accounts
    expect(createUser).toHaveBeenCalledWith('bob', [
      { gameName: 'GamerOne', tagLine: 'EUNE' }
    ])

    // Success message shows
    expect(wrapper.text()).toContain('User created successfully')

    // onCreate gets called with payload
    expect(onCreate).toHaveBeenCalled()

    // Simulate the delayed close
    await vi.advanceTimersByTimeAsync(800)
    expect(onClose).toHaveBeenCalled()
  })
})
