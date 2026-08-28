let _toast = null;

export const setToast = (toast) => {
  _toast = toast;
};

export const getToast = () => _toast;
