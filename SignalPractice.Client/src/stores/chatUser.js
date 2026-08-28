/*
   定義 pinia 的聊天用戶設定
*/
export const useChatUserStore = defineStore('chatUser', {
  state: () => ({
    // 用戶資料
    userProfile: { chatPartnerId: null, userName: null, userHeadshot: null },
  }),
  persist: true,
});
