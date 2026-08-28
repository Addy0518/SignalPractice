import * as validators from '@vuelidate/validators';

const { helpers } = validators;

export const required = helpers.withMessage('Value 為必填欄位', validators.required);

export const vaildEmail = helpers.withMessage(
  '請符合電子郵件格式 a123@gmail.com',
  helpers.regex(/^[^\s@]+@[^\s@]+\.[^\s@]+$/),
);

export const vaildLoginPassword = helpers.withMessage(
  '總共 8 個字，只能輸入英文跟數字，第一個字要大寫',
  helpers.regex(/^[A-Z][A-Za-z0-9]{7}$/),
);

export const verfiyEnAndSymbolAndNum = helpers.withMessage(
  '僅限輸入英數字及特殊符號',
  helpers.regex(/^[\w~!@#$%^&*()-_./\\]+$/),
);

export const vaildNumber = helpers.withMessage('僅限輸入數字', helpers.regex(/^[0-9]*$/));

export const minLength = (len) =>
  helpers.withMessage(`至少 ${len} 個字元`, validators.minLength(len));

export const maxLength = (len) =>
  helpers.withMessage(`最多 ${len} 個字元`, validators.maxLength(len));

export const vaildCellPhone = helpers.withMessage(
  '請符合手機號碼格式 0912345678',
  helpers.regex(/^09[\d]{8}$/),
);

export const vaildUnifiedNumber = helpers.withMessage(
  '統一編號為 8 碼數字',
  helpers.regex(/^\d{8}$/),
);

export const sameAsPassword = (newPasswordRef) =>
  helpers.withMessage('兩次輸入的密碼不一致', (value) => value === newPasswordRef.value);
