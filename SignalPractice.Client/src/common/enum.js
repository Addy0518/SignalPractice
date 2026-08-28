/**
 * 取得 Enum 的描述
 * @param {Object} enumObj enums.js 中的 enum 物件
 * @param {Number} value enum 的值
 * @returns
 */
export const getEnumDescription = (enumObj, value) => {
  const key = Object.keys(enumObj).find((key) => enumObj[key].value === value);
  if (key) return enumObj[key].description;
  else {
    console.error(`enum value '${value}' is undefined`);
    return value;
  }
};

// Http 回傳狀態碼
export const httpCodeStatusEnum = Object.freeze({
  Ok: 200,
  BadRequest: 400,
  Unauthorized: 401,
  Forbidden: 403,
  NotFound: 404,
  MethodNotAllowed: 405,
  ManyRequest: 429,
  InternalServerError: 500,
  ServiceUnavailable: 503,
});

// 自訂回傳狀態碼
export const codeStatusEnum = Object.freeze({
  // 成功
  Success: 2000,
  // Request驗證失敗
  RequestError: 4000,
  // 查無資料
  NotFound: 4001,
  // 內部伺服器錯誤
  InternalException: 5000,
});

// 刪除狀態
export const isDeleteEnum = Object.freeze({
  Normal: { value: 0, description: '正常' },
  Delete: { value: 1, description: '軟刪除' },
});

// 性別
export const genderEnum = Object.freeze({
  Man: { value: 0, description: '女性' },
  Woman: { value: 1, description: '男性' },
  Else: { value: 2, description: '其他' },
});

// 商品運送狀態
export const shippingEnum = Object.freeze({
  PendingPayment: { value: 0, description: '待付款' },
  PendingShipment: { value: 1, description: '待出貨' },
  InTransit: { value: 2, description: '運送中' },
  Arrived: { value: 3, description: '已抵達門市' },
  Completed: { value: 4, description: '已完成取貨' },
  Cancelled: { value: 5, description: '已取消' },
});

// 物流單詳細 狀態
export const logisticsEnum = Object.freeze({
  Created: { value: 0, description: '物流單已建立' },
  PendingShipment: { value: 1, description: '付款完成，等待出貨' },
  Shipped: { value: 2, description: '賣家已出貨' },
  InTransit: { value: 3, description: '配送中' },
  Delivered: { value: 4, description: '已送達門市/已配達' },
  PickedUp: { value: 5, description: '買家已取件' },
  Cancelled: { value: 6, description: '已取消' },
  Exception: { value: 7, description: '異常，需人工處理' },
});

// 審核狀態
export const reviewStatusEnum = Object.freeze({
  All: { value: null, description: '全部' },
  Pending: { value: 0, description: '待審核' },
  Approved: { value: 1, description: '審核通過' },
  Reject: { value: 2, description: '駁回申請' },
});

// 優惠卷類別
export const couponTypeEnum = Object.freeze({
  百分比折扣: { value: 0, description: '百分比折扣' },
  固定金額折抵: { value: 1, description: '固定金額折抵' },
  免運券: { value: 2, description: '免運券' },
  商品特價券: { value: 3, description: '商品特價券' },
});

// 通知類別
export const notificationTypeEnum = Object.freeze({
  ProductApproved: { value: 1, description: '商品審核通過' },
  ProductRejected: { value: 2, description: '商品審核駁回' },
  StoreCompanyApproved: { value: 3, description: '企業賣場審核通過' },
  StoreCompanyRejected: { value: 4, description: '企業賣場審核駁回' },
  LogisticsStatusUpdated: {
    value: 5,
    description: '訂單物流狀態更新 ( 已出貨 / 已送達 / 買家已取貨 )',
  },
  NewOrder: { value: 6, description: '賣家收到新訂單' },
  ProductRateReplied: { value: 7, description: '賣家評論被回覆 ( 或評論收到賣家回覆，通知買家 )' },
  ProductUnderReview: { value: 8, description: '商品審核中' },
  LogisticsCreateFailed: { value: 9, description: '訂單物流建立失敗' },
  ProductListing: { value: 10, description: '商品上架' },
});
