import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/vue'
import App from '@/App.vue'

describe('App.vue', () => {
  it('renders the root container and a router-view', () => {
    render(App, {
      global: {
        stubs: { RouterView: { template: '<div data-testid="router-view" />' } }
      }
    })

    // Root container exists
    const root = screen.getByTestId('app-root')
    expect(root).toBeTruthy()

    // RouterView is rendered
    expect(screen.getByTestId('router-view')).toBeTruthy()
  })

  it('mounts without errors even with a minimal RouterView stub', () => {
    render(App, {
      global: {
        stubs: { RouterView: { template: '<div data-testid="rv" />' } }
      }
    })
    expect(screen.getByTestId('app-root')).toBeTruthy()
    expect(screen.getByTestId('rv')).toBeTruthy()
  })
})
