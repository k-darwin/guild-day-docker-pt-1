// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2024-11-01',
  ssr: false,
  devtools: { enabled: true },
  css: [
    '~/assets/scss/main.scss'
  ],
  runtimeConfig: {
    public: {
      apiGateWayEndpoint: process.env.GATEWAY_ENDPOINT || 'http://localhost:8280/',
    }
  }
})
