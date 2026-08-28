import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      /*
         測試白板
      */
      path: '/',
      name: 'whiteBoard',
      // meta: {
      //   isPermissionVerification: false,
      // },
      component: () => import('@/views/WhiteBoard.vue'),
    },
  ],
})

export default router
