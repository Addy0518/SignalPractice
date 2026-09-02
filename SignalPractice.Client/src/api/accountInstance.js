import axios from 'axios'
import { useAuthStore } from '@/stores/auth'
import router from '@/router'
import { getToast } from '@/common/toast'
import { httpCodeStatusEnum } from '../common/enum'
/*
   創建一個 axios ( VITE_BASE_URL 是放在環境設定 ( env ) 的 url )
*/
const instance = axios.create({
  baseURL: import.meta.env.VITE_BASE_URL,
})

/*
   Api 發送請求的攔截器
*/
instance.interceptors.request.use(
  function (config) {
    /*
      統一把所有 api header 加上 token
    */
    const authStore = useAuthStore()
    if (authStore.token) {
      config.headers.Authorization = `Bearer ${authStore.token}`
    }
    /*
      記得 return
    */
    return config
  },
  function (error) {
    /*
       用 Promise.reject 回傳 error 給每一個 Api 的 try-catch error
    */
    return Promise.reject(error)
  },
)

/*
   Api 回傳資料的攔截器
*/
instance.interceptors.response.use(
  function (response) {
    return response
  },
  function (error) {
    // 取得全域的 toast
    const toast = getToast()

    // 網路完全斷線的情況
    if (!error.response) {
      toast.add({ severity: 'error', summary: '網路異常', detail: '請確認網路連線', life: 5000 })
      return Promise.reject(error)
    }

    const { status, data } = error.response
    // 管理各種 api 錯誤
    switch (status) {
      case httpCodeStatusEnum.BadRequest:
        const errorMsg = Object.values(data.errors)
          .flatMap((x) => x)
          .join('\n')
        toast.add({ severity: 'error', summary: '驗證錯誤', detail: errorMsg, life: 3000 })
        break

      case httpCodeStatusEnum.Unauthorized:
        toast.add({ severity: 'error', summary: '登入過期', detail: '請重新登入', life: 5000 })
        router.push({ name: 'loginView' })
        break
      case httpCodeStatusEnum.ManyRequest:
        toast.add({ severity: 'warn', summary: '請求過於頻繁', detail: '請稍後再試', life: 5000 })
        router.push({ name: 'loginView' })
        break
      case httpCodeStatusEnum.Forbidden:
        toast.add({ severity: 'error', summary: '認證失敗', detail: '你沒有訪問權限', life: 5000 })
        router.push({ name: 'loginView' })
        break
      case httpCodeStatusEnum.InternalServerError:
        const msg = `${data.message} | ${data.error500?.title}`
        toast.add({ severity: 'error', summary: '伺服器錯誤', detail: msg, life: 5000 })
        break
      case httpCodeStatusEnum.ServiceUnavailable:
        toast.add({
          severity: 'error',
          summary: '伺服器異常',
          detail: '無法連線到伺服器',
          life: 5000,
        })
        break

      default:
        toast.add({ severity: 'error', summary: `未知錯誤 (${status})`, detail: '', life: 5000 })
        break
    }
    return Promise.reject(error)
  },
)
/*
   記得 export 出去
*/
export default instance
