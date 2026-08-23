// 跨页面传递"下拉刷新"回调的轻量总线：
// 列表页（统计/历史）在挂载时注册自己的刷新函数，主布局的滚动容器在用户下拉时触发它。
// 非列表页不注册，下拉刷新自动不生效。

type RefreshFn = () => Promise<void> | void

let current: RefreshFn | null = null

/** 注册当前页面的下拉刷新处理函数，返回取消注册的函数 */
export function registerPullRefresh(fn: RefreshFn): () => void {
  current = fn
  return () => {
    if (current === fn) current = null
  }
}

/** 触发当前页面的下拉刷新（无注册页时为空操作） */
export function triggerPullRefresh(): Promise<void> {
  if (!current) return Promise.resolve()
  return Promise.resolve(current())
}

/** 当前是否存在可下拉刷新的页面 */
export function hasPullRefresh(): boolean {
  return current !== null
}
