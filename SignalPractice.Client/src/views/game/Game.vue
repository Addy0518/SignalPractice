<script setup>
import { getRoomInfoAPI } from '@/api/roomService'
import { useGameResultStore } from '@/stores/gameResult'
import { getAvatarEmoji } from '@/common/avatar'

// ============================================================
// 前端流程 ( 跟後端 Hub 流程對應 )
//
// 1. onMouted 初始化建立連線 ( startConnection )
//    => 把所有 conn.on 監聽器註冊好
//    => 拿到房間資料
//    => 呼叫後端 Hub 方法加入 Signal 群組
//
// 2. DrawerChoosing
//    => 廣播告訴房間裡所有人誰在選字
//    => 如果有人上一輪的回合結束彈窗還沒關閉的話 , 暫存資料到 pendingNextEvent
//
// 3. YourChoosingTopic
//    => 廣播給畫家選題目
//    => 開始這回合的選題目計時 , 沒有就自動選題目
//
// 4. 畫家選擇題目後
//    => chooseWord 拿到題目結果
//
// 5. GameStarted
//    => 遊戲開始
//    => 跟第 2 步一樣 , 彈窗還沒關閉的話 , 暫存資料到 pendingNextEvent
//
// 6. 畫圖階段（ SendDraw / ReceiveDraw / ClearDraw )
//    => isDrawer 判斷是不是畫家 , 畫的線即時廣播給其他人
//
// 7. 猜題（ sendGuess → GuessCorrect / GuessInCorrect ）
//    => 猜對：更新分數、顯示短暫的加分動畫
//    => 猜錯：推進留言區
//
// 8. RoundEnd ( 這回合結束 )
//    => 顯示結束彈窗 , 開著期間收到的 DrawerChoosing / GameStarted / GameEnd 都會先暫存到 pendingNextEvent，等彈窗關閉那一刻才套用
//
// 9. GameEnd ( 遊戲結束 )
//    => 把排名資訊存 Pinia 並導到 Result.vue
// ============================================================

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const gameResultStore = useGameResultStore()
const showToastSuccess = inject('showToastSuccess')
const showToastError = inject('showToastError')

const roomCode = route.params.code
const roomInfo = ref()
const players = ref([])
const messages = ref([])
const guessInput = ref('')
const myTopic = ref('')
const showRoundEnd = ref(false)
const roundEndData = ref(null)
const currentDrawerId = ref(null)
const currentRound = ref(0)
const totalRound = ref(0)
const timeLeft = ref(0)
let timerInterval = null
const showTools = ref(false)
let roundEndTimer = null
let pendingNextEvent = null

const showChoosing = ref(false)
const wordChoices = ref([])
const choosingDrawerName = ref('')
const chooseTimeLeft = ref(10)
let chooseTimerInterval = null
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
   前端選題目倒計時動畫
*/
const startChooseTimer = () => {
  clearInterval(chooseTimerInterval)
  chooseTimeLeft.value = 10
  // setInterval 一樣是 Js 自帶 , 設定每幾秒執行一次 , 直到 clearInterval
  chooseTimerInterval = setInterval(() => {
    chooseTimeLeft.value = Math.max(0, chooseTimeLeft.value - 1)
    if (chooseTimeLeft.value <= 0) clearInterval(chooseTimerInterval)
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

  // 等待選擇的題目（一樣只有畫畫的人收到）
  conn.on('YourChoosingTopic', (data) => {
    wordChoices.value = data
    // 開始到計時選題目的時間
    startChooseTimer()
  })

  // 畫家選題目
  conn.on('DrawerChoosing', (data) => {
    if (showRoundEnd.value) {
      pendingNextEvent = { type: 'DrawerChoosing', data }
    } else {
      applyDrawerChoosing(data)
    }
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

  // 收到畫線的資料並推到畫布上
  conn.on('ReceiveDraw', (data) => {
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
    // 停止畫面上的倒數計時
    clearInterval(timerInterval)
    lines.value = []
    myTopic.value = ''

    // 存下這回合結束時的資訊
    roundEndData.value = data
    // 顯示彈窗
    showRoundEnd.value = true

    // 防止上一輪殘留的計時器還在跑 ( 保險 )
    clearTimeout(roundEndTimer)

    // 計時器 , 設定為五秒關閉彈窗
    roundEndTimer = setTimeout(() => {
      showRoundEnd.value = false
      // pendingNextEvent 這裡存的是類型 ( GameStarted / GameEnd ) 跟比賽資訊 ( data )
      if (pendingNextEvent) {
        const evt = pendingNextEvent
        // 存完清空 , 避免重複用
        pendingNextEvent = null
        // 看接下來是要換畫家選題目並下一回合還是結束遊戲
        if (evt.type === 'DrawerChoosing') applyDrawerChoosing(evt.data)
        else if (evt.type === 'GameEnd') applyGameEnd(evt.data)
      }
    }, 5000)
  })

  // 下一輪開始
  conn.on('GameStarted', (data) => {
    if (showRoundEnd.value) {
      pendingNextEvent = { type: 'GameStarted', data }
    } else {
      applyGameStarted(data)
    }
  })

  // 遊戲結束
  conn.on('GameEnd', (data) => {
    if (showRoundEnd.value) {
      pendingNextEvent = { type: 'GameEnd', data: data }
    } else {
      applyGameEnd(data)
    }
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
  clearTimeout(roundEndTimer)
  clearInterval(chooseTimerInterval)
  stopConnection()
})

/*
   下一輪遊戲開始
*/
const applyGameStarted = (data) => {
  // 題目選好就關掉畫面跟清空舊題目
  showChoosing.value = false
  wordChoices.value = []
  clearInterval(chooseTimerInterval)

  // 載入這回合的資料
  currentDrawerId.value = data.drawerId
  currentRound.value = data.currentRound
  timeLeft.value = data.roundSeconds
  messages.value = []
  startTimer(data.roundEndTime)
}

/*
   畫家選題目中的畫面
*/
const applyDrawerChoosing = (data) => {
  currentDrawerId.value = data.drawerId
  currentRound.value = data.currentRound
  totalRound.value = data.totalRound
  choosingDrawerName.value = data.drawerName
  showChoosing.value = true
}

/*
   遊戲結束
*/
const applyGameEnd = (data) => {
  // 把分數 , 排名等資料存進 pinia , 再去 result 頁面秀分數
  gameResultStore.setRankings(data?.rankings || [])
  router.push({
    name: 'result',
    params: { code: roomCode },
  })
}

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
   選擇題目
*/
const chooseWord = async (wordBankId) => {
  const conn = getConnection()
  if (conn) await conn.invoke('ChooseWord', roomInfo.value.roomId, wordBankId)
  wordChoices.value = []
  clearInterval(chooseTimerInterval)
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
    <!--#region 這輪結束彈窗 -->
    <div
      v-if="showRoundEnd"
      class="fixed inset-0 bg-black/45 flex items-center justify-center z-50"
    >
      <div class="bg-white rounded-2xl p-9 w-150 text-center">
        <p class="text-xs font-extrabold text-slate-400 tracking-widest mb-2">這輪結束</p>
        <p class="text-sm text-slate-500 mb-1">答案是</p>
        <p class="text-2xl font-black text-indigo-500 mb-5">{{ roundEndData?.word }}</p>
        <div class="flex flex-col gap-2 text-left mb-5">
          <div
            v-for="s in [...(roundEndData?.scores || [])].sort((a, b) => b.score - a.score)"
            :key="s.playerId"
            class="flex items-center gap-2.5 bg-slate-50 rounded-xl px-3 py-2.5"
          >
            <div
              class="w-7 h-7 rounded-full bg-indigo-50 border-2 border-indigo-200 flex items-center justify-center text-sm shrink-0"
            >
              😊
            </div>
            <span class="flex-1 text-xs font-extrabold text-slate-700">{{ s.playerName }}</span>
            <span class="text-xs font-extrabold text-indigo-400">{{ s.score }} 分</span>
          </div>
        </div>
        <p class="text-xs text-slate-400">下一輪即將開始…</p>
      </div>
    </div>
    <!-- #endregion -->
    <!--#region 選字階段 -->
    <div
      v-if="showChoosing"
      class="fixed inset-0 bg-black/45 flex items-center justify-center z-50"
    >
      <!-- 畫家：候選題目 -->
      <div v-if="isDrawer && wordChoices.length" class="bg-white rounded-2xl p-9 w-150 text-center">
        <p class="text-xs font-extrabold text-slate-400 tracking-widest mb-2">輪到你畫畫了</p>
        <p class="text-sm text-slate-500 mb-5">選一個題目吧（{{ chooseTimeLeft }} 秒）</p>
        <div class="flex flex-col gap-2.5">
          <button
            v-for="w in wordChoices"
            :key="w.workBankId"
            @click="chooseWord(w.workBankId)"
            class="py-3.5 rounded-xl border-2 border-slate-200 bg-slate-50 font-extrabold text-base text-slate-700 hover:bg-indigo-50 hover:border-indigo-300 cursor-pointer transition-colors"
          >
            {{ w.word }}
          </button>
        </div>
      </div>

      <!-- 其他人：等待畫家選字 -->
      <div v-else class="bg-white rounded-2xl p-9 w-96 text-center">
        <p class="text-xs font-extrabold text-slate-400 tracking-widest mb-2">請稍候</p>
        <p class="text-lg font-black text-indigo-500">{{ choosingDrawerName }} 正在選題目…</p>
      </div>
    </div>
    <!-- #endregion -->
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
                {{ getAvatarEmoji(player.playerId) }}
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
