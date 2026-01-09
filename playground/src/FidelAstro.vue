<template>
  <div class="astro-wrapper">
    <div class="astro-bg"></div>
    <div class="astro-overlay"></div>

    <header class="astro-header">
      <div class="brand">
        <span class="brand-mark">✦</span>
        <h1 class="brand-title">Fidel Astro</h1>
      </div>
      <p class="brand-sub">Astrology by Lea Isobell Jacobasen</p>
      <div class="brand-icons">
        <svg class="icon raven" viewBox="0 0 64 64" aria-hidden="true">
          <path d="M8 40c10-6 18-10 26-8 6 2 10 2 16-4 4-4 6-6 6-10-6 2-12 0-18-4-8-6-16-8-22-4-4 2-6 6-6 10 0 6 2 10 6 14 2 2 0 6-8 10z" fill="currentColor" />
          <circle cx="45" cy="22" r="2" fill="currentColor" />
        </svg>
        <svg class="icon skull" viewBox="0 0 64 64" aria-hidden="true">
          <circle cx="32" cy="26" r="16" fill="currentColor" />
          <circle cx="26" cy="24" r="4" fill="#0b0b0e" />
          <circle cx="38" cy="24" r="4" fill="#0b0b0e" />
          <rect x="26" y="36" width="12" height="6" rx="2" fill="#0b0b0e" />
        </svg>
      </div>
      <button class="cta">Get Your Zodiac Reading</button>
    </header>

    <section class="astro-section features">
      <div class="feature-card">
        <h3>Personalized Zodiac</h3>
        <p>Precise natal insights guided by your birth chart.</p>
      </div>
      <div class="feature-card">
        <h3>Constellation Mapping</h3>
        <p>See your sign drawn in the stars with live sky data.</p>
      </div>
      <div class="feature-card">
        <h3>Modern Witchcraft</h3>
        <p>Rituals and guidance with a refined, contemporary aesthetic.</p>
      </div>
    </section>

    

    <footer class="astro-footer">
      <p>© {{ year }} Fidel Astro · Crafted under the night sky</p>
    </footer>
  </div>
</template>

<script setup>
import { ref } from 'vue'
const year = new Date().getFullYear()
</script>

<style scoped>
.astro-wrapper {
  --purple: #6d28d9;
  --purple-2: #4c1d95;
  --teal: #2dd4bf;
  --ink: #0b0b0e;
  --text: #e7e7ee;
  --muted: #a1a1b3;
  position: relative;
  min-height: 100vh;
  color: var(--text);
  background: var(--ink);
  overflow: hidden;
}

.astro-bg {
  position: fixed;
  inset: 0;
  background-image: url('/fidel.png');
  background-size: cover;
  background-position: center;
  filter: brightness(0.45) contrast(1.1);
  z-index: 0;
}

.astro-overlay {
  position: fixed;
  inset: 0;
  background: radial-gradient(1200px 600px at 10% 10%, rgba(109, 40, 217, 0.35), transparent 60%),
              radial-gradient(1000px 500px at 90% 20%, rgba(45, 212, 191, 0.25), transparent 65%),
              linear-gradient(180deg, rgba(10, 10, 14, 0.85), rgba(10, 10, 14, 0.95));
  pointer-events: none;
  z-index: 1;
}

.astro-header {
  position: relative;
  z-index: 2;
  max-width: 1100px;
  margin: 0 auto;
  padding: 4rem 1.5rem 2rem;
  text-align: center;
}

.brand { display: inline-flex; align-items: baseline; gap: 0.75rem; }
.brand-mark { color: var(--teal); font-size: 1.25rem; }
.brand-title {
  font-size: 3rem;
  font-weight: 800;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  background: linear-gradient(90deg, #c4b5fd, #93c5fd);
  -webkit-background-clip: text;
  background-clip: text;
  color: transparent;
}
.brand-sub { color: var(--muted); margin-top: 0.5rem; }

.brand-icons { display: flex; justify-content: center; gap: 1.25rem; margin: 1.25rem 0 2rem; }
.icon { width: 54px; height: 54px; color: #c4b5fd; filter: drop-shadow(0 8px 16px rgba(109,40,217,.25)); }
.icon.raven { color: #93c5fd; }
.icon.skull { color: #a78bfa; }

.cta {
  background: linear-gradient(90deg, var(--purple), var(--purple-2));
  color: white;
  border: 1px solid rgba(255,255,255,0.15);
  border-radius: 999px;
  padding: 0.9rem 1.4rem;
  font-weight: 700;
  letter-spacing: 0.05em;
  box-shadow: 0 12px 32px rgba(109, 40, 217, 0.35);
  cursor: pointer;
}
.cta:hover { filter: brightness(1.1); box-shadow: 0 16px 44px rgba(109, 40, 217, 0.45); }

.astro-section { position: relative; z-index: 2; max-width: 1100px; margin: 0 auto; padding: 2rem 1.5rem 3rem; }
.features { display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 1rem; }
.feature-card {
  background: rgba(20, 20, 28, 0.6);
  border: 1px solid rgba(168, 85, 247, 0.25);
  border-radius: 16px;
  padding: 1.25rem;
  backdrop-filter: blur(8px);
  box-shadow: 0 12px 28px rgba(0,0,0,0.35);
}
.feature-card h3 { font-size: 1.125rem; margin-bottom: 0.35rem; }
.feature-card p { color: var(--muted); }

.section-title { text-align: center; font-size: 1.75rem; margin-bottom: 1rem; }
.zodiac-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 1rem; }
.z-card {
  position: relative;
  background: rgba(20, 20, 28, 0.6);
  border: 1px solid rgba(45, 212, 191, 0.25);
  border-radius: 14px;
  padding: 1rem;
  min-height: 140px;
  overflow: hidden;
}
.z-name { position: absolute; bottom: 0.75rem; left: 0.75rem; font-weight: 700; color: #c7f9cc; }

.constellation {
  position: absolute;
  inset: 0.75rem;
  background-image: radial-gradient(#e6fbff 2px, transparent 2px), radial-gradient(#e6fbff 2px, transparent 2px);
  background-size: 16px 16px, 24px 24px;
  background-position: 10% 20%, 80% 60%;
  opacity: 0.4;
}

.astro-footer { position: relative; z-index: 2; text-align: center; color: var(--muted); padding: 2rem 1rem 3rem; }
</style>
