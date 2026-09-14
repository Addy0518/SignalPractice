<script setup>
import { getRoomInfoAPI } from '@/api/roomService'
import { getAvatarEmoji } from '@/common/avatar'
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const showLoading = inject('showLoading')
const hideLoading = inject('hideLoading')
const showToastSuccess = inject('showToastSuccess')
const showToastError = inject('showToastError')

const roomCode = route.params.code
const roomInfo = ref(null)
const players = ref([])

/*
   是否為房主
*/
const isHost = computed(() => roomInfo.value?.roomOwnerId === authStore.userId)

/*
   計算房間剩餘的空位
   Math.max 取最大數 ( 在 0 和 6 - 玩家人數 之間，取數值比較大的那一個（最大值）, 假設玩家近來兩個就是 0 , 6-2=4 , 取 4 )
*/
const emptySlots = computed(() => Math.max(0, 6 - players.value.length))
/*
    初始化
*/
onMounted(async () => {
  try {
    showLoading()

    // 取得房間資訊
    const infoRes = await getRoomInfoAPI(roomCode)
    if (infoRes.data.codeStatus === 2000) {
      roomInfo.value = infoRes.data.returnData
      players.value = infoRes.data.returnData.players
    }

    // 建立 SignalR 連線
    await startConnection()
    const conn = getConnection()

    // 監聽有人加入
    conn.on('PlayerJoined', (player) => {
      const exists = players.value.find((p) => p.playerId === player.playerId)
      if (!exists) players.value.push(player)
    })

    // 監聽有人離開
    conn.on('PlayerLeft', (playerId) => {
      players.value = players.value.filter((p) => p.playerId !== playerId)
    })

    // 監聽遊戲開始 → 跳到遊戲頁面
    conn.on('GameStarted', () => {
      router.push({ name: 'game', params: { code: roomCode } })
    })

    await conn.invoke('JoinRoom', roomInfo.value.roomId)
  } catch (err) {
    console.error(err)
  } finally {
    hideLoading()
  }
})

/*
    開始遊戲（房主才能按）
  */
const startGame = async () => {
  const conn = getConnection()
  if (!conn || conn.state !== 'Connected') {
    showToastError('連線未就緒，請稍候...')
    return
  }
  await conn.invoke('StartGame', roomInfo.value.roomId)
}

/*
    離開房間
*/
const leaveRoom = async () => {
  const conn = getConnection()
  if (conn) await conn.invoke('LeaveRoom', roomInfo.value.roomId)
  router.push({ name: 'loginView' })
}
</script>

<template>
  <div
    class="min-h-screen flex flex-col items-center justify-center px-6 py-10"
    style="background: linear-gradient(160deg, #74b9ff 0%, #a29bfe 100%)"
  >
    <!--#region 返回按鈕 -->
    <button
      @click="leaveRoom"
      class="self-start mb-4 text-white/80 font-extrabold text-sm flex items-center gap-1 hover:text-white cursor-pointer bg-transparent border-none"
    >
      ← 離開
    </button>
    <!-- #endregion -->

    <!--#region 標題 -->
    <h1
      class="text-3xl font-black text-white mb-1"
      style="text-shadow: 0 2px 0 rgba(0, 0, 0, 0.15)"
    >
      等待室
    </h1>
    <p class="text-white/70 text-xs font-extrabold tracking-widest mb-6">等待玩家加入</p>
    <!-- #endregion -->

    <div class="bg-white rounded-2xl shadow-xl w-full max-w-md p-8">
      <!--#region 房間代碼 -->
      <div
        class="bg-slate-50 border-2 border-dashed border-indigo-200 rounded-xl p-4 text-center mb-6"
      >
        <p class="text-3xl font-black text-indigo-500 tracking-widest">{{ roomCode }}</p>
        <p class="text-xs text-slate-400 font-bold mt-1">把代碼分享給朋友，邀請他們加入！</p>
      </div>
      <!-- #endregion -->

      <!--#region 房間資訊 -->
      <div class="grid grid-cols-3 gap-3 mb-6" v-if="roomInfo">
        <div class="bg-slate-50 border border-slate-200 rounded-xl p-3 text-center">
          <p class="text-xl font-black text-slate-700">{{ roomInfo.totalRound }}</p>
          <p class="text-xs font-extrabold text-slate-400 mt-1">總輪數</p>
        </div>
        <div class="bg-slate-50 border border-slate-200 rounded-xl p-3 text-center">
          <p class="text-xl font-black text-slate-700">{{ roomInfo.roundSeconds }}s</p>
          <p class="text-xs font-extrabold text-slate-400 mt-1">每輪時間</p>
        </div>
        <div class="bg-slate-50 border border-slate-200 rounded-xl p-3 text-center">
          <p class="text-xl font-black text-slate-700">{{ players.length }}/6</p>
          <p class="text-xs font-extrabold text-slate-400 mt-1">玩家人數</p>
        </div>
      </div>
      <!-- #endregion -->

      <!--#region 玩家列表 -->
      <p class="text-xs font-extrabold text-slate-400 tracking-widest mb-3">玩家列表</p>
      <div class="flex flex-col gap-2 mb-6">
        <div
          v-for="player in players"
          :key="player.playerId"
          class="flex items-center gap-3 px-3 py-2.5 bg-slate-50 rounded-xl"
        >
          <div
            class="w-8 h-8 rounded-full bg-indigo-50 border-2 border-indigo-200 flex items-center justify-center text-base shrink-0"
          >
            {{ getAvatarEmoji(player.playerId) }}
          </div>
          <span class="flex-1 text-sm font-extrabold text-slate-700">{{ player.playerName }}</span>
          <span
            v-if="player.playerId === roomInfo?.roomOwnerId"
            class="text-xs font-extrabold text-indigo-400 bg-indigo-50 px-2 py-0.5 rounded-full"
          >
            房主
          </span>
        </div>

        <!--#region 空位 -->
        <div
          v-for="i in emptySlots"
          :key="'empty-' + i"
          class="flex items-center gap-3 px-3 py-2.5 border-2 border-dashed border-slate-200 rounded-xl"
        >
          <div
            class="w-8 h-8 rounded-full border-2 border-dashed border-slate-300 flex items-center justify-center text-slate-300 text-sm shrink-0"
          >
            +
          </div>
          <span class="text-sm font-extrabold text-slate-300">等待玩家加入...</span>
        </div>
        <!-- #endregion -->
      </div>
      <!-- #endregion -->

      <!--#region 按鈕 -->
      <button
        v-if="isHost"
        @click="startGame"
        :disabled="players.length < 2"
        class="w-full py-4 rounded-xl font-extrabold text-base border-b-4 cursor-pointer transition-opacity"
        :class="
          players.length >= 2
            ? 'bg-emerald-400 text-white border-emerald-600 hover:opacity-90'
            : 'bg-slate-200 text-slate-400 border-slate-300 cursor-not-allowed'
        "
      >
        {{ players.length >= 2 ? '開始遊戲 ! ' : '至少需要 2 位玩家' }}
      </button>

      <div
        v-else
        class="w-full py-4 rounded-xl bg-indigo-50 text-indigo-400 font-extrabold text-base text-center border-2 border-indigo-100"
      >
        等待房主開始遊戲...
      </div>
      <!-- #endregion -->
    </div>
  </div>
</template>
