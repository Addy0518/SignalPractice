<script setup>
import { getRecordDetailsInfoAPI } from '@/api/userService'
import { getAvatarEmoji } from '@/common/avatar'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const showLoading = inject('showLoading')
const hideLoading = inject('hideLoading')
const showToastError = inject('showToastError')

const detail = ref(null)

onMounted(async () => {
  try {
    showLoading()
    const res = await getRecordDetailsInfoAPI(route.params.id)
    if (res.data.codeStatus === 2000) {
      detail.value = res.data.returnData
    } else {
      router.push({ name: 'records' })
    }
  } catch (err) {
    console.error(err)
    showToastError('讀取失敗')
  } finally {
    hideLoading()
  }
})

const formatTime = (iso) => new Date(iso + 'Z').toLocaleString('zh-TW')
const rankLabel = (rank) => (rank === 1 ? '🥇' : rank === 2 ? '🥈' : rank === 3 ? '🥉' : `${rank}`)
</script>

<template>
  <div
    class="min-h-screen flex flex-col items-center px-6 py-10"
    style="background: linear-gradient(160deg, #74b9ff 0%, #a29bfe 100%)"
  >
    <button
      @click="router.push({ name: 'records' })"
      class="self-start mb-4 text-white/80 font-extrabold text-sm hover:text-white cursor-pointer bg-transparent border-none"
    >
      ← 返回列表
    </button>

    <div v-if="detail" class="bg-white rounded-2xl shadow-xl w-full max-w-md p-8">
      <h1 class="text-xl font-black text-indigo-500 text-center mb-1">{{ detail.roomName }}</h1>
      <p class="text-xs font-bold text-slate-400 text-center mb-6">
        {{ formatTime(detail.createTime) }} · {{ detail.totalRound }} 輪 · 每輪
        {{ detail.roundSeconds }} 秒
      </p>

      <div class="flex flex-col gap-2">
        <div
          v-for="p in detail.players"
          :key="p.playerId"
          class="flex items-center gap-3 px-3 py-2.5 rounded-xl"
          :class="
            p.playerId === authStore.userId
              ? 'bg-indigo-50 border-2 border-indigo-200'
              : 'bg-slate-50'
          "
        >
          <div class="w-8 text-center font-black text-slate-500">{{ rankLabel(p.rank) }}</div>
          <div
            class="w-8 h-8 rounded-full bg-indigo-50 border-2 border-indigo-200 flex items-center justify-center shrink-0"
          >
            {{ getAvatarEmoji(p.playerId) }}
          </div>
          <span class="flex-1 text-sm font-extrabold text-slate-700 truncate">
            {{ p.playerName }}
            <span v-if="p.playerId === authStore.userId" class="text-xs text-indigo-400"
              >（你）</span
            >
          </span>
          <span class="text-sm font-extrabold text-indigo-400">{{ p.score }} 分</span>
        </div>
      </div>
    </div>
  </div>
</template>
