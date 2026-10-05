<script setup>
import { getGameRecordInfoAPI } from '@/api/userService'

const router = useRouter()
const showLoading = inject('showLoading')
const hideLoading = inject('hideLoading')

const records = ref([])

onMounted(async () => {
  try {
    showLoading()
    const res = await getGameRecordInfoAPI()
    if (res.data.codeStatus === 2000) {
      records.value = res.data.returnData
    }
  } catch (err) {
    console.error(err)
  } finally {
    hideLoading()
  }
})

// 後端存的是 UTC，顯示時轉成本地時間
const formatTime = (iso) => new Date(iso + 'Z').toLocaleString('zh-TW')

const rankLabel = (rank) => (rank === 1 ? '🥇' : rank === 2 ? '🥈' : rank === 3 ? '🥉' : `第 ${rank} 名`)
</script>

<template>
  <div
    class="min-h-screen flex flex-col items-center px-6 py-10"
    style="background: linear-gradient(160deg, #74b9ff 0%, #a29bfe 100%)"
  >
    <button
      @click="router.push({ name: 'loginView' })"
      class="self-start mb-4 text-white/80 font-extrabold text-sm hover:text-white cursor-pointer bg-transparent border-none"
    >
      ← 回首頁
    </button>

    <h1 class="text-3xl font-black text-white mb-6" style="text-shadow: 0 2px 0 rgba(0, 0, 0, 0.15)">
      📜 我的歷史紀錄
    </h1>

    <div class="bg-white rounded-2xl shadow-xl w-full max-w-2xl p-6">
      <p v-if="records.length === 0" class="text-center text-slate-400 font-extrabold py-10">
        還沒有遊戲紀錄，快去玩一場吧！
      </p>

      <div v-else class="flex flex-col gap-2.5">
        <div
          v-for="r in records"
          :key="r.gameRecordId"
          @click="router.push({ name: 'recordDetail', params: { id: r.gameRecordId } })"
          class="flex items-center gap-3 px-4 py-3 bg-slate-50 rounded-xl cursor-pointer hover:bg-indigo-50 transition-colors"
        >
          <div class="text-xl w-14 text-center shrink-0">{{ rankLabel(r.rank) }}</div>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-extrabold text-slate-700 truncate">{{ r.roomName }}</p>
            <p class="text-xs font-bold text-slate-400">
              {{ formatTime(r.createTime) }} · {{ r.totalRound }} 輪
            </p>
          </div>
          <div class="text-sm font-extrabold text-indigo-400">{{ r.score }} 分</div>
        </div>
      </div>
    </div>
  </div>
</template>
