// TextBanner 标签桥接（Manifest V3 service worker）
// 在标签创建/更新/关闭/切换时，把当前所有标签推送给本机程序。
const PORT = 51739;
const ENDPOINT = `http://127.0.0.1:${PORT}/tabs`;

function browserName() {
  const ua = navigator.userAgent || '';
  if (ua.includes('Edg/')) return 'msedge';
  if (ua.includes('Chrome/')) return 'chrome';
  if (ua.includes('Brave')) return 'brave';
  if (ua.includes('Vivaldi')) return 'vivaldi';
  if (ua.includes('OPR/')) return 'opera';
  if (ua.includes('Firefox/')) return 'firefox';
  return 'chrome';
}

async function push() {
  try {
    const tabs = await chrome.tabs.query({});
    const payload = {
      browser: browserName(),
      tabs: tabs
        .filter((t) => typeof t.id === 'number')
        .map((t) => ({ id: t.id, title: t.title || '', url: t.url || '' }))
    };
    await fetch(ENDPOINT, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
      keepalive: true
    });
  } catch (e) {
    // 本程序未运行或端口未监听时静默忽略
  }
}

chrome.tabs.onCreated.addListener(() => push());
chrome.tabs.onUpdated.addListener((id, info) => {
  if (info.title !== undefined || info.url !== undefined || info.status === 'complete') return push();
});
chrome.tabs.onRemoved.addListener(() => push());
chrome.tabs.onActivated.addListener(() => push());
chrome.tabs.onAttached.addListener(() => push());
chrome.tabs.onDetached.addListener(() => push());
chrome.tabs.onReplaced.addListener(() => push());

// 心跳：即使 worker 被回收，事件也会重新唤醒并刷新状态
setInterval(push, 3000);
