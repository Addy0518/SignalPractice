<script setup>
import { registerAPI } from '@/api/userService'

const router = useRouter()

const showLoading = inject('showLoading')
const hideLoading = inject('hideLoading')
const showToastSuccess = inject('showToastSuccess')
const showToastError = inject('showToastError')

const account = ref('')
const password = ref('')
const userName = ref('')

/*
  驗證規則
*/
const rules = computed(() => ({
  account: { required, vaildEmail },
  password: { required, vaildLoginPassword },
  userName: { required, maxLength: maxLength(50) },
}))

const v$ = useVuelidate(rules, { account, password, userName }, { $lazy: true, $scope: false })

/*
  註冊
*/
const userRegister = async () => {
  const isValid = await v$.value.$validate()
  if (!isValid) return

  try {
    showLoading()
    const res = await registerAPI({
      userAccount: account.value,
      userPassword: password.value,
      userName: userName.value,
    })
    const { data } = res
    if (data.codeStatus === 2000) {
      showToastSuccess('註冊成功，請登入')
      router.push({ name: 'loginView' })
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
    <!--#region 標題 -->
    <h1
      class="text-4xl font-black text-white mb-1"
      style="text-shadow: 0 2px 0 rgba(0, 0, 0, 0.15)"
    >
      🎨 你畫我猜
    </h1>
    <p class="text-white/70 text-xs font-extrabold tracking-widest mb-6">建立你的帳號</p>
    <!-- #endregion -->

    <div class="bg-white rounded-2xl shadow-xl w-full max-w-md p-8">
      <div class="flex flex-col gap-5">
        <!--#region 遊戲暱稱 -->
        <div>
          <label class="block text-xs font-extrabold text-slate-400 tracking-widest mb-2">
            遊戲暱稱
          </label>
          <input
            v-model="userName"
            type="text"
            placeholder="請輸入遊戲暱稱"
            class="w-full border-2 rounded-xl px-4 py-3 text-sm font-bold text-slate-700 outline-none transition-colors"
            :class="
              v$.userName.$error
                ? 'border-red-400 focus:border-red-400'
                : 'border-slate-200 focus:border-indigo-400'
            "
          />
          <InValidErrorMessage :errorDto="v$.userName.$errors" vaildChiName="遊戲暱稱" />
        </div>
        <!-- #endregion -->

        <!--#region 帳號 -->
        <div>
          <label class="block text-xs font-extrabold text-slate-400 tracking-widest mb-2">
            帳號（Email）
          </label>
          <input
            v-model="account"
            type="text"
            placeholder="請輸入 Email"
            class="w-full border-2 rounded-xl px-4 py-3 text-sm font-bold text-slate-700 outline-none transition-colors"
            :class="
              v$.account.$error
                ? 'border-red-400 focus:border-red-400'
                : 'border-slate-200 focus:border-indigo-400'
            "
          />
          <InValidErrorMessage :errorDto="v$.account.$errors" vaildChiName="帳號（ Email )" />
        </div>
        <!-- #endregion -->

        <!--#region 密碼 -->
        <div>
          <label class="block text-xs font-extrabold text-slate-400 tracking-widest mb-2">
            密碼
          </label>
          <input
            v-model="password"
            type="password"
            placeholder="8碼，第一個字大寫，含英文和數字"
            class="w-full border-2 rounded-xl px-4 py-3 text-sm font-bold text-slate-700 outline-none transition-colors"
            :class="
              v$.password.$error
                ? 'border-red-400 focus:border-red-400'
                : 'border-slate-200 focus:border-indigo-400'
            "
          />
          <InValidErrorMessage :errorDto="v$.password.$errors" vaildChiName="密碼" />
        </div>
        <!-- #endregion -->
      </div>

      <!--#region 按鈕 -->
      <div class="mt-8 flex flex-col gap-3">
        <button
          @click="userRegister"
          class="w-full py-3 rounded-xl bg-indigo-400 text-white font-extrabold text-sm border-b-4 border-indigo-600 hover:opacity-90 cursor-pointer transition-opacity"
        >
          建立帳號
        </button>
        <p class="text-center text-xs text-slate-400 font-bold">
          已有帳號？
          <span
            @click="router.push({ name: 'loginView' })"
            class="text-indigo-400 cursor-pointer hover:underline"
          >
            返回登入
          </span>
        </p>
      </div>
      <!-- #endregion -->
    </div>
  </div>
</template>
