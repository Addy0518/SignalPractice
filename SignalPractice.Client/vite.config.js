import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import AutoImport from 'unplugin-auto-import/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
    tailwindcss(),
    AutoImport({
      imports: [
        'vue',
        'vue-router',
        'pinia',
        {
          '@vuelidate/core': ['useVuelidate'],
        },
      ], // vue 內建
      dirs: ['src/common', 'src/stores', 'src/validator', 'src/api'], // 我自訂的
      dts: 'src/auto-imports.d.ts',
      vueTemplate: true, // 確保 template 裡的也能自動 import
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
})
