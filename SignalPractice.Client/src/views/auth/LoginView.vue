<script setup>
import { loginAPI, guestLoginAPI } from '@/api/userService'
import { joinRoomAPI } from '@/api/roomService'
import { getAvatarEmoji } from '@/common/avatar'
const router = useRouter()
const authStore = useAuthStore()
const account = ref('')
const password = ref('')
const roomCode = ref('')
const showAccountLogin = ref(false)

const showLoading = inject('showLoading')
const hideLoading = inject('hideLoading')
const showToastSuccess = inject('showToastSuccess')
const showToastError = inject('showToastError')

const testGmail = ref('angey920518@gmail.com')
const testPassword = ref('Andy1111')
const testGmail2 = ref('bbb@gmail.com')

const testUser = () => {
  account.value = testGmail.value
  password.value = testPassword.value
}

const testUser2 = () => {
  account.value = testGmail2.value
  password.value = testPassword.value
}

/*
   加入已經寫好的驗證規則
*/
const rules = computed(() => ({
  account: { required, maxLength: maxLength(200), vaildEmail },
  password: { required, vaildLoginPassword },
}))

/*
   加入套件驗證設定
*/
const v$ = useVuelidate(rules, { account, password }, { $lazy: true, $scope: false })

/*
    遊客登入
*/
const guestLogin = async () => {
  try {
    showLoading()
    const res = await guestLoginAPI()
    const { data } = res
    if (data.codeStatus === 2000) {
      authStore.setAuth(data.returnData)
      showToastSuccess('以遊客身份登入')
    }
  } catch (err) {
    console.error(err)
  } finally {
    hideLoading()
  }
}

/*
    帳號密碼登入
*/
const userLogin = async () => {
  const isFormCorrect = await v$.value.$validate()
  if (!isFormCorrect) return

  try {
    showLoading()
    const res = await loginAPI({ account: account.value, password: password.value })
    const { data } = res
    if (data.codeStatus === 2000) {
      authStore.setAuth(data.returnData)
      showToastSuccess('登入成功')
      showAccountLogin.value = false
    }
  } catch (err) {
    console.error(err)
  } finally {
    hideLoading()
  }
}

/*
   登出
*/
const handleLogout = () => {
  authStore.clearAuth()
  showToastSuccess('已登出')
}

/*
    建立房間
*/
const createRoom = () => {
  if (!authStore.token) {
    showToastError('請先登入或以遊客身份遊玩')
    return
  }
  router.push({ name: 'create-room' })
}

/*
    加入房間
*/
const joinOneRoom = async () => {
  try {
    showLoading()
    const request = {
      RoomCode: roomCode.value.toUpperCase(),
    }
    const res = await joinRoomAPI(request)
    if (res.data.codeStatus === 2000) {
      showToastSuccess('加入成功')
      router.push({ name: 'room', params: { code: roomCode.value.toUpperCase() } })
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
    class="min-h-screen flex flex-col items-center justify-center"
    style="background: linear-gradient(160deg, #74b9ff 0%, #a29bfe 100%)"
  >
    <!--#region 登入狀態列 -->
    <div
      v-if="authStore.token"
      class="absolute top-4 right-4 bg-white/20 rounded-full pl-4 pr-1.5 py-1.5 flex items-center gap-3"
    >
      <span class="text-white font-extrabold text-sm">
        {{ getAvatarEmoji(authStore.userId) }} {{ authStore.userName }}
      </span>
      <button
        @click="handleLogout"
        class="px-3.5 py-1.5 bg-white text-indigo-500 font-extrabold text-xs rounded-full cursor-pointer hover:opacity-90"
      >
        登出
      </button>
    </div>
    <!-- #endregion -->
    <!--#region 標題 -->
    <h1
      class="text-6xl font-black text-white mb-1"
      style="text-shadow: 0 2px 0 rgba(0, 0, 0, 0.15)"
    >
      🎨 你畫我猜
    </h1>
    <p class="text-white/70 text-xs font-extrabold tracking-widest mb-6">DRAW · GUESS · WIN</p>
    <!-- #endregion -->

    <div class="bg-white rounded-2xl shadow-xl w-full max-w-4xl px-10 py-10">
      <!--#region 建立/加入房間 -->
      <div class="grid grid-cols-2 gap-6 mb-6">
        <!--#region 左側：開始遊戲 -->
        <div>
          <p class="text-xs font-extrabold text-slate-400 tracking-widest mb-3">開始遊戲</p>
          <button
            @click="createRoom"
            class="w-full py-3 rounded-xl bg-indigo-400 text-white font-extrabold text-sm border-b-4 border-indigo-600 hover:opacity-90 cursor-pointer flex items-center justify-center gap-2 mb-2.5 transition-opacity"
          >
            ➕ 建立房間
          </button>
          <button
            @click="guestLogin"
            class="w-full py-3 rounded-xl bg-slate-50 text-slate-500 font-extrabold text-sm border-2 border-slate-200 border-b-4 border-b-slate-300 hover:bg-slate-100 cursor-pointer flex items-center justify-center gap-2 transition-colors"
          >
            👤 遊客遊玩
          </button>
        </div>
        <!-- #endregion -->

        <!--#region 右側：加入房間 -->
        <div>
          <p class="text-xs font-extrabold text-slate-400 tracking-widest mb-3">加入房間</p>
          <input
            v-model="roomCode"
            type="text"
            placeholder="ABC123"
            maxlength="6"
            @keyup.enter="joinOneRoom"
            class="w-full border-2 border-slate-200 rounded-xl px-3 py-2.5 text-center text-base font-extrabold text-slate-700 outline-none focus:border-emerald-400 tracking-widest uppercase mb-2.5 transition-colors"
          />
          <button
            @click="joinOneRoom"
            class="w-full py-3 rounded-xl bg-emerald-400 text-white font-extrabold text-sm border-b-4 border-emerald-600 hover:opacity-90 cursor-pointer transition-opacity"
          >
            加入 →
          </button>
        </div>
        <!-- #endregion -->
      </div>
      <!-- #endregion -->

      <!--#region 登入 -->
      <div class="border-t border-slate-100 pt-5">
        <p class="text-xs font-extrabold text-slate-400 tracking-widest mb-3">以其他方式登入</p>
        <div class="flex gap-3">
          <!--#region Google 登入按鍵-->
          <button
            class="flex-1 py-3 rounded-xl border-2 border-slate-200 bg-slate-50 font-extrabold text-sm text-slate-600 hover:bg-slate-100 cursor-pointer flex items-center justify-center gap-2 transition-colors"
          >
            <div
              class="w-5 h-5 rounded border border-dashed border-slate-300 flex items-center justify-center text-xs text-slate-400 font-black"
            >
              G
            </div>
            Google 登入
          </button>
          <!-- #endregion -->

          <!--#region 帳號密碼登入按鍵 -->
          <button
            @click="showAccountLogin = !showAccountLogin"
            class="flex-1 py-3 rounded-xl border-2 border-slate-200 bg-slate-50 font-extrabold text-sm text-slate-600 hover:bg-slate-100 cursor-pointer flex items-center justify-center gap-2 transition-colors"
          >
            <div
              class="w-5 h-5 rounded border border-dashed border-slate-300 flex items-center justify-center text-xs text-slate-400 font-black"
            >
              帳
            </div>
            帳號密碼
          </button>
          <!-- #endregion -->
        </div>

        <!--#region 帳號跟密碼欄位 -->
        <div v-if="showAccountLogin" class="mt-4 flex flex-col gap-2.5">
          <!--#region 測試帳號 -->
          <button
            class="flex-1 py-3 rounded-xl border-2 border-slate-200 bg-slate-50 font-extrabold text-sm text-slate-600 hover:bg-slate-100 cursor-pointer flex items-center justify-center gap-2 transition-colors"
            @click="testUser"
          >
            測試帳號 1
          </button>
          <button
            class="flex-1 py-3 rounded-xl border-2 border-slate-200 bg-slate-50 font-extrabold text-sm text-slate-600 hover:bg-slate-100 cursor-pointer flex items-center justify-center gap-2 transition-colors"
            @click="testUser2"
          >
            測試帳號 2
          </button>
          <!-- #endregion -->
          <!--#region 帳號 -->
          <div>
            <label class="block text-xs font-extrabold text-slate-400 tracking-widest mb-2"
              >帳號（Email）</label
            >
            <input
              v-model="account"
              type="text"
              placeholder="請輸入 Email"
              class="w-full border-2 border-slate-200 rounded-xl px-4 py-3 text-sm font-bold text-slate-700 outline-none focus:border-indigo-400 transition-colors"
              :class="v$.account.$error ? 'border-red-400' : ''"
            />
            <InValidErrorMessage :errorDto="v$.account.$errors" vaildChiName="帳號" />
          </div>
          <!-- #endregion -->

          <!--#region 密碼 -->
          <div>
            <label class="block text-xs font-extrabold text-slate-400 tracking-widest mb-2"
              >密碼</label
            >
            <input
              v-model="password"
              type="password"
              placeholder="8碼，第一個字大寫，含英文和數字"
              class="w-full border-2 border-slate-200 rounded-xl px-4 py-3 text-sm font-bold text-slate-700 outline-none focus:border-indigo-400 transition-colors"
              :class="v$.password.$error ? 'border-red-400' : ''"
            />
            <InValidErrorMessage :errorDto="v$.password.$errors" vaildChiName="密碼" />
          </div>
          <!-- #endregion -->
          <button
            @click="userLogin"
            class="w-full py-3 rounded-xl bg-indigo-400 text-white font-extrabold text-sm border-b-4 border-indigo-600 hover:opacity-90 cursor-pointer transition-opacity"
          >
            登入
          </button>
          <p class="text-center text-xs text-slate-400 font-bold">
            還沒有帳號？
            <span
              @click="router.push({ name: 'registerView' })"
              class="text-indigo-400 cursor-pointer hover:underline"
            >
              註冊
            </span>
          </p>
        </div>
        <!-- #endregion -->
      </div>
      <!-- #endregion -->
    </div>
  </div>
</template>
