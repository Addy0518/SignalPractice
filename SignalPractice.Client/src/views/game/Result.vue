<script setup>
import { useGameResultStore } from '@/stores/gameResult'
import { getAvatarEmoji } from '@/common/avatar'
const route = useRoute()
const router = useRouter()
const gameResultStore = useGameResultStore()
const roomCode = route.params.code
const rankings = computed(() => gameResultStore.rankings)

const first = computed(() => rankings.value[0])
const second = computed(() => rankings.value[1])

/*
   回等待室
*/
const backToRoom = () => {
  gameResultStore.clear()
  router.push({ name: 'room', params: { code: roomCode } })
}

/*
   離開，回到登入首頁
*/
const leave = () => {
  gameResultStore.clear()
  router.push({ name: 'loginView' })
}
</script>

<template>
  <div
    class="min-h-screen flex flex-col items-center justify-center px-6 py-10"
    style="background: linear-gradient(160deg, #74b9ff 0%, #a29bfe 100%)"
  >
    <div class="bg-indigo-500 rounded-3xl p-10 w-full max-w-xl text-center">
      <p class="text-xs font-extrabold text-indigo-100 tracking-widest mb-1">遊戲結束</p>
      <h1 class="text-2xl font-black text-white mb-7">最終排行榜</h1>

      <!--#region 頒獎台（前兩名） -->
      <div v-if="rankings.length" class="flex items-end justify-center gap-3 mb-6">
        <!-- 第二名 -->
        <div v-if="second" class="flex flex-col items-center gap-1.5">
          <div
            class="w-10 h-10 rounded-full bg-white flex items-center justify-center text-sm font-extrabold text-slate-500"
          >
            {{ getAvatarEmoji(second.playerId) }}
          </div>
          <p class="text-xs font-extrabold text-white truncate max-w-20">{{ second.playerName }}</p>
          <p class="text-xs font-extrabold text-indigo-100">{{ second.score }} 分</p>
          <div class="w-20 h-13 bg-white/25 rounded-t-lg flex items-start justify-center pt-1.5">
            <span class="text-xs font-extrabold text-white">2</span>
          </div>
        </div>

        <!-- 第一名 -->
        <div v-if="first" class="flex flex-col items-center gap-1.5">
          <span class="text-xl">👑</span>
          <div
            class="w-13 h-13 rounded-full bg-white flex items-center justify-center text-base font-extrabold text-indigo-500"
          >
            {{ getAvatarEmoji(first.playerId) }}
          </div>
          <p class="text-sm font-extrabold text-white truncate max-w-24">{{ first.playerName }}</p>
          <p class="text-xs font-extrabold text-indigo-100">{{ first.score }} 分</p>
          <div class="w-20 h-22 bg-white/35 rounded-t-lg flex items-start justify-center pt-1.5">
            <span class="text-sm font-extrabold text-white">1</span>
          </div>
        </div>
      </div>
      <!-- #endregion -->

      <!--#region 完整排行清單 -->
      <div v-if="rankings.length" class="bg-white rounded-2xl p-1.5 text-left mb-6">
        <div
          v-for="(p, i) in rankings"
          :key="p.playerId"
          class="flex items-center gap-2.5 px-3 py-2.5"
          :class="i !== rankings.length - 1 ? 'border-b border-slate-100' : ''"
        >
          <span
            class="w-5 text-xs font-extrabold shrink-0"
            :class="p.rank === 1 ? 'text-amber-500' : 'text-slate-400'"
          >
            {{ p.rank }}
          </span>
          <div
            class="w-7 h-7 rounded-full bg-indigo-50 border-2 border-indigo-200 flex items-center justify-center text-sm shrink-0"
          >
            {{ getAvatarEmoji(p.playerId) }}
          </div>
          <span class="flex-1 text-xs font-extrabold text-slate-700 truncate">{{
            p.playerName
          }}</span>
          <span class="text-xs font-extrabold text-indigo-500">{{ p.score }} 分</span>
        </div>
      </div>

      <!-- 沒有排行榜資料時的保底顯示（例如重新整理頁面遺失了 state） -->
      <div v-else class="text-white/80 text-sm font-bold mb-6">找不到排行榜資料</div>
      <!-- #endregion -->

      <!--#region 按鈕 -->
      <div class="flex gap-2.5">
        <button
          @click="backToRoom"
          class="flex-1 py-3.5 rounded-xl bg-white text-indigo-500 font-extrabold text-sm cursor-pointer hover:opacity-90"
        >
          回等待室
        </button>
        <button
          @click="leave"
          class="flex-1 py-3.5 rounded-xl bg-white/20 text-white font-extrabold text-sm cursor-pointer hover:bg-white/30"
        >
          離開
        </button>
      </div>
      <!-- #endregion -->
    </div>
  </div>
</template>
