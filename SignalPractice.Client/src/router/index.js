import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'loginView',
      component: () => import('@/views/auth/LoginView.vue'),
    },
    {
      path: '/registerView',
      name: 'registerView',
      component: () => import('@/views/auth/RegisterView.vue'),
    },
    {
      path: '/room/:code',
      name: 'room',
      meta: { requiresAuth: true },
      component: () => import('@/views/room/Room.vue'),
    },
    {
      path: '/create-room',
      name: 'create-room',
      meta: { requiresAuth: true },
      component: () => import('@/views/room/CreateRoom.vue'),
    },
    {
      path: '/game/:code',
      name: 'game',
      meta: { requiresAuth: true },
      component: () => import('@/views/game/Game.vue'),
    },
    {
      path: '/game/:code/result',
      name: 'result',
      meta: { requiresAuth: true },
      component: () => import('@/views/game/Result.vue'),
    },
  ],
})

router.beforeEach((to, from, next) => {
  const auth = localStorage.getItem('auth')
  const token = auth ? JSON.parse(auth)?.token : null

  if (to.meta.requiresAuth && !token) {
    next({ name: 'loginView' })
  } else {
    next()
  }
})

export default router
