import * as signalR from '@microsoft/signalr';

/*
  connection 存放目前的 Signal 連線物件
  null 代表還未建立連線
*/
let connection = null;

/*
  建立連線並回傳連線物件
*/
export const startConnection = async () => {
  const authStore = useAuthStore();
  const userId = authStore.userId;
  // HubConnectionBuilder 是 SignalR 提供的建構器，用來設定連線細節
  connection = new signalR.HubConnectionBuilder()
    // withUrl : 設定要連線的後端 Hub 網址 ( 不是 API 喔 )
    .withUrl(`http://localhost:5215/chatHub?userId=${userId}`, {
      // 第二個參數是額外設定 , 這裡把 JWT token 帶進去，後端才能驗證
      accessTokenFactory: () => authStore.token,
    })
    // withAutomaticReconnect：網路斷線時自動重連
    .withAutomaticReconnect()
    // build：依照上面的設定，正式建立連線物件（但還沒真正連線）
    .build();

  // connection.start()：真正發起連線，跟後端建立 WebSocket
  await connection.start();

  // 最後回傳這筆連線的物件 , 讓呼叫人可以使用
  return connection;
};

/*
  在 export 一個自訂方法來丟出去這個連線物件 , 這樣就像 api 一樣可以給其他 component 用了
*/
export const getConnection = () => connection;

/*
  關閉連線並把連線物件清空
*/
export const stopConnection = async () => {
  if (connection) {
    await connection.stop();
    connection = null;
  }
};
