/*
   定義 pinia 的權限設定
*/
export const useAuthStore = defineStore(
  'auth',
  () => {
    const token = ref(null);
    const userId = ref(null);
    const userName = ref(null);
    const userHeadshot = ref(null);
    const userPhone = ref(null);
    const userAddress = ref(null);
    const userZipCode = ref(null);
    const userRole = ref(null);
    /*
      登入時存入 token 資料
    */
    function setAuth(data) {
      token.value = data.token;
      userId.value = data.userId;
      userName.value = data.userName;
      userHeadshot.value = data.userHeadshot;
      userPhone.value = data.userPhone;
      userAddress.value = data.userAddress;
      userZipCode.value = data.userZipCode;
      userRole.value = data.userRole;
    }
    /*
      登出時清除
    */
    function clearAuth() {
      token.value = null;
      userId.value = null;
      userName.value = null;
      userHeadshot.value = null;
      userPhone.value = null;
      userAddress.value = null;
      userZipCode.value = null;
      userRole.value = null;
    }
    /*
      重要 => 記得回傳這些資料並 export 傳出去 , 供其他地方使用
    */
    return {
      token,
      userId,
      userName,
      userHeadshot,
      userRole,
      userPhone,
      userAddress,
      userZipCode,
      setAuth,
      clearAuth,  
    };
  },
  /*
    使用 pinia-plugin-persistedstate , 自動把 pinia 的資料存到 localstorage
  */
  {
    persist: true,
  },
);
