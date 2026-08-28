<script setup>
import { registerApi } from '@/api/userService';
/*
    變數名稱代表意義
    route : 獲取路由資訊
    account : 帳號
    password : 密碼
    name : 名稱
    tooglePassword　：　切換密碼顯示或隱藏
  */
const route = useRouter();
const account = ref();
const password = ref();
const name = ref();
const tooglePassword = ref(true);

/*
    注入 Loading 跟 Toast
  */
const showLoading = inject('showLoading');
const hideLoading = inject('hideLoading');
const showToastSuccess = inject('showToastSuccess');
const showToastError = inject('showToastError');

/*
    加入已經寫好的驗證規則
  */
const rules = computed(() => ({
  account: { required, maxLength: maxLength(200), vaildEmail },
  password: { required, vaildLoginPassword },
  name: { required, maxLength: maxLength(50) },
}));

/*
    加入套件驗證設定
  */
const v$ = useVuelidate(
  rules,
  { account, password, name, phone, address },
  { $autoDirty: true, $lazy: true, $scope: false },
);

/*
    呼叫註冊使用者 API
  */
const userRegister = async () => {
  // 要儲存前先驗證
  const isFormCorrect = await v$.value.$validate();
  if (!isFormCorrect) return;
  try {
    showLoading();

    // 把城市/地區/地址 組合成完整地址
    const detailAddress = address.value
      ? `${zipInfo.value.city}${zipInfo.value.district}${address.value}`
      : '';

    const userRegisterData = {
      userAccount: account.value,
      userPassword: password.value,
      userName: name.value,
      userPhone: phone.value,
      userAddress: detailAddress,
      userZipCode: zipInfo.value.zipCode,
    };

    const res = await registerApi(userRegisterData);
    const { data } = res;
    if (data.codeStatus === 2000) {
      showToastSuccess('註冊成功!');
      route.push('/login');
    } else if (data.codeStatus === 4000) {
      showToastError('錯誤', getError400Message(data.error400));
    }
  } catch (error) {
    console.error('使用者註冊錯誤 ', error.response);
  } finally {
    hideLoading();
  }
};
</script>

<template>
  <div class="bg-page-bg-soft py-10 px-4">
    <div class="max-w-md mx-auto">
      <!-- #region 註冊卡片 -->
      <div class="bg-page-bg border border-border-soft rounded-card p-6">
        <!-- #region 標題 -->
        <div class="text-center mb-8">
          <h1 class="text-2xl font-bold text-ink-900 m-0">建立帳號</h1>

          <p class="text-sm text-ink-500 mt-2 mb-0">註冊會員開始購物</p>
        </div>
        <!-- #endregion -->

        <!-- #region 註冊欄位 -->
        <div class="flex flex-col gap-4">
          <!-- 帳號 -->
          <div>
            <label class="block text-sm font-medium text-ink-900 mb-2"> 帳號 </label>

            <InputGroup>
              <InputGroupAddon>
                <i class="pi pi-user"></i>
              </InputGroupAddon>

              <InputText v-model="account" placeholder="請輸入帳號" :invalid="v$.account.$error" />
            </InputGroup>

            <InValidErrorMessage :errorDto="v$.account.$errors" vaildChiName="帳號" />
          </div>

          <!-- 密碼 -->
          <div>
            <label class="block text-sm font-medium text-ink-900 mb-2"> 密碼 </label>

            <InputGroup>
              <InputGroupAddon>
                <i class="pi pi-lock"></i>
              </InputGroupAddon>

              <InputText
                :type="tooglePassword ? 'password' : 'text'"
                v-model="password"
                placeholder="請輸入密碼"
                :invalid="v$.password.$error"
              />

              <InputGroupAddon class="cursor-pointer" @click="tooglePassword = !tooglePassword">
                <i :class="['pi', tooglePassword ? 'pi-eye-slash' : 'pi-eye']" />
              </InputGroupAddon>
            </InputGroup>

            <InValidErrorMessage :errorDto="v$.password.$errors" vaildChiName="密碼" />
          </div>

          <!-- 姓名 -->
          <div>
            <label class="block text-sm font-medium text-ink-900 mb-2"> 姓名 </label>

            <InputGroup>
              <InputGroupAddon>
                <i class="pi pi-id-card"></i>
              </InputGroupAddon>

              <InputText v-model="name" placeholder="請輸入姓名" :invalid="v$.name.$error" />
            </InputGroup>

            <InValidErrorMessage :errorDto="v$.name.$errors" vaildChiName="名稱" />
          </div>

          <!-- 電話 -->
          <div>
            <label class="block text-sm font-medium text-ink-900 mb-2"> 電話 </label>

            <InputGroup>
              <InputGroupAddon>
                <i class="pi pi-phone"></i>
              </InputGroupAddon>

              <InputText v-model="phone" placeholder="請輸入電話" :invalid="v$.phone.$error" />
            </InputGroup>

            <InValidErrorMessage :errorDto="v$.phone.$errors" vaildChiName="電話" />
          </div>

          <!-- 地址 : 縣市/鄉鎮市區 下拉 + 詳細地址輸入 -->
          <div>
            <label class="block text-sm font-medium text-ink-900 mb-2"> 地址 </label>

            <ZipCodeSelector v-model="zipInfo" class="mb-2" />

            <InputGroup>
              <InputGroupAddon>
                <i class="pi pi-home"></i>
              </InputGroupAddon>

              <InputText
                v-model="address"
                placeholder="請輸入詳細地址 ( 路名、門牌、樓層 )"
                :invalid="v$.address.$error"
              />
            </InputGroup>

            <InValidErrorMessage :errorDto="v$.address.$errors" vaildChiName="地址" />
          </div>
        </div>
        <!-- #endregion -->

        <!-- #region 註冊按鈕 -->
        <div class="mt-8">
          <button
            @click="userRegister"
            class="w-full py-3 rounded-card bg-brand-500 text-white font-medium cursor-pointer transition-opacity hover:opacity-90"
          >
            建立帳號
          </button>
        </div>
        <!-- #endregion -->
      </div>
      <!-- #endregion -->
    </div>
  </div>
</template>
