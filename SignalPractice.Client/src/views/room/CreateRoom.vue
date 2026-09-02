<script setup>
import { createRoomAPI } from '@/api/roomService'

const router = useRouter()
const authStore = useAuthStore()

const showLoading = inject('showLoading')
const hideLoading = inject('hideLoading')
const showToastSuccess = inject('showToastSuccess')
const showToastError = inject('showToastError')

const roomName = ref(authStore.userName + ' 的房間')
const selectedRound = ref(5)
const selectedSeconds = ref(60)

const roundOptions = [3, 5, 8, 10]
const secondsOptions = [30, 60, 90]

/*
  建立房間
*/
const createRoom = async () => {
  try {
    showLoading()
    const res = await createRoomAPI({
      roomName: roomName.value,
      totalRound: selectedRound.value,
      roundSeconds: selectedSeconds.value,
    })
    const { data } = res
    if (data.codeStatus === 2000) {
      showToastSuccess('房間建立成功')
      router.push({ name: 'room', params: { code: data.returnData } })
    }
  } catch (err) {
    console.error(err)
  } finally {
    hideLoading()
  }
}
</script>

<template>
  <div
    class="min-h-screen flex flex-col items-center justify-center px-6 py-10"
    style="background: linear-gradient(160deg, #74b9ff 0%, #a29bfe 100%)"
  >
    <!--#region 返回按鈕 -->
    <button
      @click="router.push({ name: 'lobby' })"
      class="self-start mb-4 text-white/80 font-extrabold text-sm flex items-center gap-1 hover:text-white cursor-pointer bg-transparent border-none"
    >
      ← 返回
    </button>
    <!-- #endregion -->

    <!--#region 標題 -->
    <h1
      class="text-3xl font-black text-white mb-1"
      style="text-shadow: 0 2px 0 rgba(0, 0, 0, 0.15)"
    >
      🏠 建立房間
    </h1>
    <p class="text-white/70 text-xs font-extrabold tracking-widest mb-6">設定遊戲規則</p>
    <!-- #endregion -->

    <div class="bg-white rounded-2xl shadow-xl w-full max-w-md p-8">
      <!--#region 房間名稱 -->
      <div class="mb-6">
        <p class="text-xs font-extrabold text-slate-400 tracking-widest mb-2">房間名稱</p>
        <input
          v-model="roomName"
          type="text"
          class="w-full border-2 border-slate-200 rounded-xl px-4 py-3 text-sm font-bold text-slate-700 outline-none focus:border-indigo-400 transition-colors"
        />
      </div>
      <!-- #endregion -->

      <!--#region 總輪數 -->
      <div class="mb-6">
        <p class="text-xs font-extrabold text-slate-400 tracking-widest mb-3">總輪數</p>
        <div class="flex gap-3">
          <button
            v-for="round in roundOptions"
            :key="round"
            @click="selectedRound = round"
            class="flex-1 py-3 rounded-xl font-extrabold text-sm border-2 cursor-pointer transition-all"
            :class="
              selectedRound === round
                ? 'bg-indigo-400 text-white border-indigo-400 border-b-4 border-b-indigo-600'
                : 'bg-slate-50 text-slate-500 border-slate-200 hover:bg-slate-100'
            "
          >
            {{ round }}
          </button>
        </div>
      </div>
      <!-- #endregion -->

      <!--#region 每輪時間 -->
      <div class="mb-8">
        <p class="text-xs font-extrabold text-slate-400 tracking-widest mb-3">每輪時間</p>
        <div class="flex gap-3">
          <button
            v-for="sec in secondsOptions"
            :key="sec"
            @click="selectedSeconds = sec"
            class="flex-1 py-3 rounded-xl font-extrabold text-sm border-2 cursor-pointer transition-all"
            :class="
              selectedSeconds === sec
                ? 'bg-indigo-400 text-white border-indigo-400 border-b-4 border-b-indigo-600'
                : 'bg-slate-50 text-slate-500 border-slate-200 hover:bg-slate-100'
            "
          >
            {{ sec }}秒
          </button>
        </div>
      </div>
      <!-- #endregion -->

      <!--#region 建立按鈕 -->
      <button
        @click="createRoom"
        class="w-full py-4 rounded-xl bg-indigo-400 text-white font-extrabold text-base border-b-4 border-indigo-600 hover:opacity-90 cursor-pointer transition-opacity"
      >
        建立房間 →
      </button>
      <!-- #endregion -->
    </div>
  </div>
</template>
