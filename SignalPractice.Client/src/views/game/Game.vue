<script setup>
import { getRoomInfoAPI } from '@/api/roomService'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const showToastSuccess = inject('showToastSuccess')
const showToastError = inject('showToastError')

const roomCode = route.params.code
const roomInfo = ref()
const players = ref([])
const messages = ref([])
const guessInput = ref('')
const myTopic = ref('')
const currentDrawerId = ref(null)
const currentRound = ref(0)
const totalRound = ref(0)
const timeLeft = ref(0)
const showTools = ref(false)

/*
  畫布相關
*/
const lines = ref([])
const isDrawing = ref(false)
const currentLine = ref([])
const selectedColor = ref('#1e1e1e')
const selectedSize = ref(3)
const stageConfig = ref({ width: 0, height: 0 })
const canvasWrapRef = ref(null)
let timerInterval = null
const colors = [
  '#1e1e1e',
  '#ef4444',
  '#f97316',
  '#eab308',
  '#22c55e',
  '#3b82f6',
  '#a855f7',
  '#ffffff',
]
const sizes = [
  { label: '細', value: 2 },
  { label: '中', value: 5 },
  { label: '粗', value: 10 },
]
const isDrawer = computed(() => currentDrawerId.value === authStore.userId)

const isHost = computed(() => roomInfo.value?.roomOwnerId === authStore.userId)

/*
  玩家排行（依分數排序）
  用 [...players.value] 複製一份新陣列來避免動到本來的陣列 ( 常見的防禦性寫法 , 要動陣列的話都盡量這樣寫 )
  sort 比對陣列裡的值 b - a 如果是正數 ( b 比 a 大 ) , b 就排在 a 前面 , 反之亦然
*/
const sortedPlayers = computed(() => [...players.value].sort((a, b) => b.score - a.score))

/*
   前端倒計時動畫
   因為每回合跑的時間只會在後端跑而不會顯示在螢幕上 , 所以前端要有一個動畫來告知玩家時間
   endTimeIso 也就是傳進來的參數會是 room.RoundEndTime ( 每回合預定結束時間 )
*/
const startTimer = (endTimeIso) => {
  // clearInterval Js 自帶 , 在每一輪開始前清除上回合的倒計時
  clearInterval(timerInterval)
  // 傳進來的時間字串轉日期格式
  const endTime = new Date(endTimeIso).getTime()

  // setInterval 一樣是 Js 自帶 , 設定每幾秒執行一次 , 直到 clearInterval
  timerInterval = setInterval(() => {
    // remaining 是剩餘時間
    const remaining = Math.max(0, Math.round((endTime - Date.now()) / 1000))
    timeLeft.value = remaining
    // 到時間就清空
    if (remaining <= 0) clearInterval(timerInterval)
  }, 1000)
}

/*
  初始化
*/
onMounted(async () => {
  // canvasWrapRef : canva 畫布尺寸
  // nextTick : 等 Vue 渲染完再執行 , 這樣 canvasWrapRef 才能抓到尺寸
  nextTick(() => {
    if (canvasWrapRef.value) {
      stageConfig.value = {
        width: canvasWrapRef.value.offsetWidth,
        height: canvasWrapRef.value.offsetHeight,
      }
    }
  })

  await startConnection()
  const conn = getConnection()

  // 如果斷線重連的話就再次呼叫
  registerReconnectHandler(async () => {
    await conn.invoke('JoinRoom', roomInfo.value.roomId)
    await conn.invoke('AddToUserGroup', authStore.userId)
  })

  // 題目（只有畫畫的人收到）
  conn.on('YourTopic', (data) => {
    myTopic.value = data.word
  })

  // 收到猜測的訊息並推到留言板上
  conn.on('GuessInCorrect', (data) => {
    messages.value.push({ playerName: data.playerName, content: data.content, isCorrect: false })
  })

  // 猜對
  conn.on('GuessCorrect', (data) => {
    messages.value.push({
      playerName: data.playerName,
      content: `${data.playerName} 猜對了！`,
      isCorrect: true,
    })
    // 更新分數
    const player = players.value.find((p) => p.playerId === data.playerId)
    if (player) {
      player.score = data.totalScore

      // 顯示這次的加分 , 時間一到消失
      player.lastGain = data.score
      clearTimeout(player._gainTimer)
      // 自訂一個屬性 ( _gainTimer ) , 而不是直接用 player.value , 這樣不共用一個變數避免計時器互清
      player._gainTimer = setTimeout(() => {
        player.lastGain = null
      }, 2000)
    }
  })

  // 下一輪開始
  conn.on('GameStarted', (data) => {
    currentDrawerId.value = data.drawerId
    currentRound.value = data.currentRound
    timeLeft.value = data.roundSeconds
    messages.value = []
    startTimer(data.roundEndTime)
  })

  // 收到畫線的資料並推到畫布上
  conn.on('ReceiveDraw', (data) => {
    console.log('是否有收到畫線', data)
    lines.value.push({
      points: data.points,
      stroke: data.color,
      strokeWidth: data.strokeWidth,
    })
  })

  // 清除畫布
  conn.on('ClearDraw', () => {
    lines.value = []
  })

  // 這輪結束
  conn.on('RoundEnd', (data) => {
    showToastSuccess(`這輪結束！答案是：${data.word}`)
    lines.value = []
    myTopic.value = ''
    clearInterval(timerInterval)
  })

  // 遊戲結束
  conn.on('GameEnd', () => {
    router.push({ name: 'room', params: { code: roomCode.toUpperCase() } })
  })

  // Error
  conn.on('Error', (msg) => {
    showToastError(msg)
  })

  const infoRes = await getRoomInfoAPI(roomCode)
  if (infoRes.data.codeStatus === 2000) {
    roomInfo.value = infoRes.data.returnData
    players.value = infoRes.data.returnData.players
    currentDrawerId.value = infoRes.data.returnData.currentDrawerId
    currentRound.value = infoRes.data.returnData.currentRound
    totalRound.value = infoRes.data.returnData.totalRound
    timeLeft.value = infoRes.data.returnData.roundSeconds
  }

  if (infoRes.data.returnData.roundEndTime) {
    startTimer(infoRes.data.returnData.roundEndTime)
  }

  if (!roomInfo.value) {
    showToastError('找不到房間資訊')
    router.push({ name: 'loginView' })
    return
  }

  // 先把前面的監聽 ( conn.on ) 全都設定好 , 再呼叫 Hub 方法 ( conn.invoke )
  // conn.on(...) 是裝監聽器 , 不會主動做任何事 , 只是被動等著接訊息 => conn.invoke(...) 才是真正觸發後端動作
  await conn.invoke('JoinRoom', roomInfo.value.roomId)
  await conn.invoke('AddToUserGroup', authStore.userId)
})

/*
   關閉畫面時
*/
onUnmounted(() => {
  clearInterval(timerInterval)
  stopConnection()
})

/*
  滑鼠按下時觸發
  只有 isDrawer（ 畫畫的人 ）才能畫
  記錄起始座標到 currentLine
*/
const handleMouseDown = (e) => {
  if (!isDrawer.value) return
  isDrawing.value = true
  const pos = e.target.getStage().getPointerPosition()
  currentLine.value = [pos.x, pos.y]
}

/*
  滑鼠移動時觸發
  持續把新座標加進 currentLine
  ...currentLine.value 是展開舊座標，再加新的 x, y
  這樣 currentLine 會變成 [x1,y1, x2,y2, x3,y3, ...] 的格式
  Konva 需要這種格式才能畫出連續的線
*/
const handleMouseMove = async (e) => {
  if (!isDrawing.value || !isDrawer.value) return
  const pos = e.target.getStage().getPointerPosition()
  currentLine.value = [...currentLine.value, pos.x, pos.y]
}

/*
  滑鼠放開時觸發
  停止畫線並 SendDraw 把線傳給其他玩家看
*/
const handleMouseUp = async () => {
  if (!isDrawing.value || !isDrawer.value) return
  isDrawing.value = false

  const conn = getConnection()
  if (conn) {
    await conn.invoke(
      'SendDraw',
      roomInfo.value.roomId,
      currentLine.value,
      selectedColor.value,
      selectedSize.value,
    )
  }

  lines.value.push({
    points: currentLine.value,
    stroke: selectedColor.value,
    strokeWidth: selectedSize.value,
  })
  currentLine.value = []
}

/*
  清除畫布
*/
const clearCanvas = async () => {
  lines.value = []
  const conn = getConnection()
  if (conn) await conn.invoke('ClearDraw', roomInfo.value.roomId)
}

/*
  送出猜測
*/
const sendGuess = async () => {
  if (!guessInput.value.trim()) return
  const conn = getConnection()
  if (conn) await conn.invoke('SendGuess', roomInfo.value.roomId, guessInput.value)
  guessInput.value = ''
}

/*
   離開房間
*/
const leaveRoom = async () => {
  const conn = getConnection()
  if (conn && roomInfo.value) {
    await conn.invoke('LeaveRoom', roomInfo.value.roomId)
  }
  router.push({ name: 'loginView' })
}

/*
   結束遊戲 ( 房主才能 )
*/
const endGame = async () => {
  const conn = getConnection()
  if (conn && roomInfo.value) {
    await conn.invoke('EndGame', roomInfo.value.roomId)
  }
}
</script>

<template>
  <div
    class="min-h-screen p-4 flex flex-col"
    style="background: linear-gradient(160deg, #74b9ff 0%, #a29bfe 100%)"
  >
    <!--#region 標題 , 一局的時間 , 第幾輪 -->
    <div class="flex items-center justify-between mb-3">
      <div class="text-white font-black text-xl" style="text-shadow: 0 2px 0 rgba(0, 0, 0, 0.15)">
        🎨 你畫我猜
      </div>
      <div class="bg-white rounded-full px-5 py-2 font-black text-indigo-500 text-lg">
        ⏱ {{ timeLeft }}
      </div>
      <div class="flex items-center gap-3">
        <span class="text-white/80 font-extrabold text-sm">
          第 {{ currentRound }} / {{ totalRound }} 輪
        </span>
        <!-- 只有房主看得到 -->
        <button
          v-if="isHost"
          @click="endGame"
          class="px-3 py-1.5 bg-red-400 text-white font-extrabold text-xs rounded-lg border-b-2 border-red-600 hover:opacity-90 cursor-pointer"
        >
          結束遊戲
        </button>
        <button
          @click="leaveRoom"
          class="px-3 py-1.5 bg-white/20 text-white font-extrabold text-xs rounded-lg border-b-2 border-white/30 hover:bg-white/30 cursor-pointer"
        >
          離開房間
        </button>
      </div>
    </div>
    <!-- #endregion -->

    <div class="flex gap-3 flex-1">
      <!--#region 左側：排行 + 猜題區 -->
      <div class="flex flex-col gap-3 w-70 shrink-0">
        <!--#region 玩家排行 -->
        <div class="bg-white rounded-2xl p-4">
          <p class="text-xs font-extrabold text-slate-400 tracking-widest mb-3">🏆 玩家排行</p>
          <div class="flex flex-col gap-2">
            <div
              v-for="player in sortedPlayers"
              :key="player.playerId"
              class="flex items-center gap-2 px-2 py-2 bg-slate-50 rounded-xl"
            >
              <div
                class="w-7 h-7 rounded-full bg-indigo-50 border-2 border-indigo-200 flex items-center justify-center text-sm shrink-0"
              >
                😊
              </div>
              <span class="flex-1 text-xs font-extrabold text-slate-700 truncate">{{
                player.playerName
              }}</span>
              <span v-if="player.playerId === currentDrawerId" class="text-xs text-amber-500"
                >✏️</span
              >
              <span class="text-xs font-extrabold text-indigo-400">{{ player.score }}</span>
              <span
                v-if="player.lastGain"
                class="text-xs font-extrabold text-emerald-500 animate-bounce"
              >
                +{{ player.lastGain }}
              </span>
            </div>
          </div>
        </div>
        <!-- #endregion -->

        <!--#region 猜題區 -->
        <div class="bg-white rounded-2xl p-4 flex flex-col flex-1 min-h-0">
          <p class="text-xs font-extrabold text-slate-400 tracking-widest mb-3">💬 猜題區</p>
          <div class="flex-1 overflow-y-auto flex flex-col gap-2 mb-3">
            <div
              v-for="(msg, i) in messages"
              :key="i"
              class="rounded-xl px-3 py-2"
              :class="msg.isCorrect ? 'bg-emerald-50' : 'bg-slate-50'"
            >
              <p class="text-xs font-extrabold text-slate-400 mb-0.5">{{ msg.playerName }}</p>
              <p
                class="text-xs font-bold"
                :class="msg.isCorrect ? 'text-emerald-500' : 'text-slate-700'"
              >
                {{ msg.content }}
              </p>
            </div>
          </div>
          <div class="flex gap-2">
            <input
              v-model="guessInput"
              type="text"
              placeholder="輸入猜測..."
              :disabled="isDrawer"
              @keyup.enter="sendGuess"
              class="flex-1 border-2 border-slate-200 rounded-xl px-3 py-2 text-xs font-bold text-slate-700 outline-none focus:border-indigo-400 disabled:bg-slate-100 disabled:text-slate-400"
            />
            <button
              @click="sendGuess"
              :disabled="isDrawer"
              class="px-3 py-2 bg-indigo-400 text-white font-extrabold text-xs rounded-xl border-b-2 border-indigo-600 hover:opacity-90 cursor-pointer disabled:opacity-50"
            >
              送
            </button>
          </div>
        </div>
        <!-- #endregion -->
      </div>
      <!-- #endregion -->

      <!--#region 右側：題目 + 畫布 -->
      <div class="flex flex-col gap-3 flex-1">
        <!--#region 題目列 -->
        <div class="bg-white rounded-2xl px-6 py-3 flex items-center justify-center gap-3">
          <span class="text-xs font-extrabold text-slate-400">題目：</span>
          <span
            v-if="isDrawer && myTopic"
            class="text-xl font-black text-indigo-500 tracking-widest"
          >
            {{ myTopic }}
          </span>
          <div v-else class="flex gap-2">
            <div v-for="i in myTopic?.length || 3" :key="i" class="w-5 h-1 bg-slate-300 rounded" />
          </div>
        </div>
        <!-- #endregion -->

        <!--#region 畫布 -->
        <div class="bg-white rounded-2xl flex-1 relative" ref="canvasWrapRef">
          <v-stage
            :config="stageConfig"
            @mousedown="handleMouseDown"
            @mousemove="handleMouseMove"
            @mouseup="handleMouseUp"
            :style="isDrawer ? 'cursor: crosshair' : 'cursor: default'"
          >
            <v-layer>
              <v-line
                v-for="(line, index) in lines"
                :key="index"
                :config="{
                  points: line.points,
                  stroke: line.stroke,
                  strokeWidth: line.strokeWidth,
                  lineCap: 'round',
                  lineJoin: 'round',
                }"
              />
              <v-line
                v-if="currentLine.length > 0"
                :config="{
                  points: currentLine,
                  stroke: selectedColor,
                  strokeWidth: selectedSize,
                  lineCap: 'round',
                  lineJoin: 'round',
                }"
              />
            </v-layer>
          </v-stage>

          <span
            v-if="lines.length === 0 && currentLine.length === 0"
            class="absolute inset-0 flex items-center justify-center text-slate-300 font-extrabold text-sm pointer-events-none"
          >
            ✏️ 在這裡畫畫...
          </span>

          <!--#region 工具面板開關 -->
          <button
            v-if="isDrawer"
            @click="showTools = !showTools"
            class="absolute top-3 right-3 w-9 h-9 bg-slate-100 border-2 border-slate-200 rounded-xl flex items-center justify-center text-lg cursor-pointer hover:bg-slate-200 z-10"
          >
            🎨
          </button>
          <!-- #endregion -->

          <!--#region 工具面板 -->
          <div
            v-if="showTools && isDrawer"
            class="absolute top-14 right-3 bg-white border-2 border-slate-200 rounded-2xl p-4 shadow-lg z-10 flex flex-col gap-3"
          >
            <p class="text-xs font-extrabold text-slate-400 tracking-widest">工具</p>
            <div class="flex gap-2">
              <button
                class="w-9 h-9 rounded-xl border-2 flex items-center justify-center text-base cursor-pointer"
                :class="
                  selectedSize !== 0
                    ? 'bg-indigo-50 border-indigo-300'
                    : 'bg-slate-50 border-slate-200'
                "
              >
                ✏️
              </button>
              <button
                @click="selectedColor = '#ffffff'"
                class="w-9 h-9 rounded-xl border-2 bg-slate-50 border-slate-200 flex items-center justify-center text-base cursor-pointer hover:bg-slate-100"
              >
                🩹
              </button>
              <button
                @click="clearCanvas"
                class="w-9 h-9 rounded-xl border-2 bg-slate-50 border-slate-200 flex items-center justify-center text-base cursor-pointer hover:bg-slate-100"
              >
                🗑️
              </button>
            </div>

            <p class="text-xs font-extrabold text-slate-400 tracking-widest">顏色</p>
            <div class="grid grid-cols-4 gap-2">
              <button
                v-for="color in colors"
                :key="color"
                @click="selectedColor = color"
                class="w-7 h-7 rounded-full cursor-pointer border-2 transition-transform hover:scale-110"
                :style="{ backgroundColor: color }"
                :class="
                  selectedColor === color ? 'border-indigo-500 scale-110' : 'border-transparent'
                "
              />
            </div>

            <p class="text-xs font-extrabold text-slate-400 tracking-widest">粗細</p>
            <div class="flex gap-2">
              <button
                v-for="size in sizes"
                :key="size.value"
                @click="selectedSize = size.value"
                class="flex-1 py-1.5 rounded-xl border-2 text-xs font-extrabold cursor-pointer transition-colors"
                :class="
                  selectedSize === size.value
                    ? 'bg-indigo-50 border-indigo-300 text-indigo-500'
                    : 'bg-slate-50 border-slate-200 text-slate-500 hover:bg-slate-100'
                "
              >
                {{ size.label }}
              </button>
            </div>
          </div>
          <!-- #endregion -->
        </div>
        <!-- #endregion -->
      </div>
      <!-- #endregion -->
    </div>
  </div>
</template>
