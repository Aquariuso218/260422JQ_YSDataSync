<template>
  <div class="launchpad-container">
    <div class="bento-grid">
      <!-- Bento Item: Hero Banner -->
      <div class="bento-item hero-bento">
        <div class="hero-content">
          <h1 class="hero-greeting">{{ greeting }}，{{ userInfo.nickName }}</h1>
          <p class="hero-subtitle">欢迎来到 YYCAdmin 数据集成中心，把握今日数据脉络。</p>
        </div>
        <div class="hero-clock">
          <div class="date-text">{{ currentDate }}</div>
          <div class="time-text">{{ currentTime }}</div>
        </div>
      </div>

      <!-- Bento Items: Quick Links -->
      <div 
        v-for="(item, index) in quickLinks" 
        :key="index" 
        class="bento-item nav-bento"
        :class="'size-' + item.size"
        @click="handleNavigate(item.path)"
      >
        <div class="bento-inner">
          <div class="nav-icon-wrapper" :style="{ background: item.bgColor, color: item.iconColor }">
            <el-icon class="nav-icon">
              <component :is="item.icon" />
            </el-icon>
          </div>
          <div class="nav-info">
            <h3 class="nav-title">{{ item.title }}</h3>
            <p class="nav-desc">{{ item.desc }}</p>
          </div>
          <div class="nav-arrow" :style="{ color: item.iconColor }">
            <el-icon><Right /></el-icon>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup name="index">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import useUserStore from '@/store/modules/user';
import { Document, Connection, Warning, Setting, List, Bell, Right } from '@element-plus/icons-vue';

const router = useRouter();
const userInfo = computed(() => useUserStore().userInfo);

// Date & Time logic
const currentDate = ref('');
const currentTime = ref('');
const greeting = ref('');
let timer = null;

const updateTime = () => {
  const now = new Date();
  const hours = now.getHours();

  if (hours < 6) greeting.value = '夜深了';
  else if (hours < 9) greeting.value = '早上好';
  else if (hours < 12) greeting.value = '上午好';
  else if (hours < 14) greeting.value = '中午好';
  else if (hours < 18) greeting.value = '下午好';
  else greeting.value = '晚上好';

  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const date = String(now.getDate()).padStart(2, '0');
  const days = ['日', '一', '二', '三', '四', '五', '六'];
  currentDate.value = `${year}年${month}月${date}日 星期${days[now.getDay()]}`;

  currentTime.value = now.toLocaleTimeString('zh-CN', { hour12: false });
};

onMounted(() => {
  updateTime();
  timer = setInterval(updateTime, 1000);
});

onUnmounted(() => {
  if (timer) clearInterval(timer);
});

// size: 'large' (横向占据两格), 'tall' (纵向占据两格), 'small' (一格)
const quickLinks = ref([
  { title: '业务单据', desc: '查询中台单据流转与状态', icon: Document, bgColor: '#e8f4ff', iconColor: '#1890ff', path: '/business/efmidysbilldata', size: 'large' },
  { title: '异常监控', desc: '处理抓取失败记录', icon: Warning, bgColor: '#fff0f0', iconColor: '#f5222d', path: '/monitor/job', size: 'tall' },
  { title: '接口日志', desc: '查看API请求历史', icon: Connection, bgColor: '#eafaf1', iconColor: '#52c41a', path: '/monitor/operlog', size: 'small' },
  { title: '公告通知', desc: '内部平台消息下发', icon: Bell, bgColor: '#e6f7ff', iconColor: '#13c2c2', path: '/system/notice', size: 'small' },
  { title: '数据字典', desc: '系统枚举属性配置', icon: List, bgColor: '#f4f0ff', iconColor: '#722ed1', path: '/system/dict', size: 'large' },
  { title: '系统配置', desc: '全局定时任务及参数', icon: Setting, bgColor: '#fff7e6', iconColor: '#fa8c16', path: '/system/config', size: 'small' }
]);

const handleNavigate = (path) => {
  router.push(path);
};
</script>

<style scoped lang="scss">
.launchpad-container {
  padding: 30px;
  background-color: transparent;
  min-height: calc(100vh - 84px);
  display: flex;
  justify-content: center;
}

/* Bento Box Grid System */
.bento-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  grid-auto-rows: 160px; // 基础网格高度
  gap: 24px;
  width: 100%;
  max-width: 1400px;
}

/* Base Item Styles */
.bento-item {
  background: var(--el-bg-color-overlay);
  border-radius: 24px;
  overflow: hidden;
  position: relative;
  transition: all 0.4s cubic-bezier(0.16, 1, 0.3, 1);
  box-shadow: 0 4px 15px rgba(0, 0, 0, 0.03);

  &:hover {
    transform: translateY(-4px) scale(1.01);
    box-shadow: 0 16px 32px rgba(0, 0, 0, 0.08);
  }
}

/* Specific Sizes */
.hero-bento {
  grid-column: span 4;
  grid-row: span 2;
  background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
  color: #fff;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0 60px;

  &::before {
    content: '';
    position: absolute;
    inset: 0;
    background: radial-gradient(circle at 80% -20%, rgba(56, 189, 248, 0.15) 0%, transparent 50%),
                radial-gradient(circle at 10% 120%, rgba(139, 92, 246, 0.15) 0%, transparent 50%);
    pointer-events: none;
  }
}

.size-large {
  grid-column: span 2;
  grid-row: span 1;
}

.size-tall {
  grid-column: span 1;
  grid-row: span 2;
  
  .bento-inner {
    flex-direction: column !important;
    justify-content: center;
    align-items: flex-start !important;
    text-align: left;
    
    .nav-icon-wrapper { margin-bottom: 20px; }
    .nav-arrow { 
      position: absolute; 
      bottom: 24px; 
      right: 24px; 
    }
  }
}

.size-small {
  grid-column: span 1;
  grid-row: span 1;
}

/* Hero Content */
.hero-content {
  z-index: 1;
  max-width: 60%;

  .hero-greeting {
    font-size: 42px;
    font-weight: 800;
    margin: 0 0 16px 0;
    letter-spacing: -0.5px;
    background: linear-gradient(to right, #ffffff, #e2e8f0);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
  }

  .hero-subtitle {
    font-size: 18px;
    margin: 0;
    color: #94a3b8;
    line-height: 1.6;
  }
}

.hero-clock {
  z-index: 1;
  text-align: right;

  .time-text {
    font-size: 64px;
    font-weight: 700;
    font-family: 'Inter', 'Helvetica Neue', sans-serif;
    letter-spacing: -2px;
    line-height: 1;
    margin-top: 8px;
    text-shadow: 0 4px 20px rgba(0,0,0,0.5);
  }
  
  .date-text {
    font-size: 16px;
    color: #cbd5e1;
    letter-spacing: 1px;
  }
}

/* Nav Item Content */
.nav-bento {
  cursor: pointer;
  
  .bento-inner {
    padding: 32px;
    height: 100%;
    display: flex;
    align-items: center;
    box-sizing: border-box;
    position: relative;
  }

  .nav-icon-wrapper {
    width: 56px;
    height: 56px;
    border-radius: 16px;
    display: flex;
    justify-content: center;
    align-items: center;
    margin-right: 20px;
    flex-shrink: 0;
    transition: transform 0.3s ease;

    .nav-icon {
      font-size: 28px;
    }
  }

  &:hover .nav-icon-wrapper {
    transform: scale(1.1) rotate(-5deg);
  }

  .nav-info {
    flex: 1;

    .nav-title {
      margin: 0 0 6px 0;
      font-size: 18px;
      font-weight: 700;
      color: var(--el-text-color-primary);
    }

    .nav-desc {
      margin: 0;
      font-size: 13px;
      color: var(--el-text-color-secondary);
      line-height: 1.5;
    }
  }

  .nav-arrow {
    opacity: 0;
    transform: translateX(-10px);
    transition: all 0.3s ease;
    font-size: 20px;
  }

  &:hover .nav-arrow {
    opacity: 1;
    transform: translateX(0);
  }
}

/* Responsive adjustments */
@media (max-width: 1200px) {
  .bento-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .hero-bento {
    grid-column: span 2;
    padding: 40px;
  }
  .size-large { grid-column: span 2; }
  .size-tall { grid-column: span 1; grid-row: span 1; 
    .bento-inner { flex-direction: row !important; align-items: center !important; }
    .nav-icon-wrapper { margin-bottom: 0 !important; margin-right: 20px; }
  }
}

@media (max-width: 768px) {
  .bento-grid {
    grid-template-columns: 1fr;
    grid-auto-rows: auto;
  }
  .hero-bento {
    grid-column: span 1;
    flex-direction: column;
    text-align: center;
    padding: 30px 20px;
    
    .hero-content {
      max-width: 100%;
      margin-bottom: 24px;
    }
    .hero-clock { text-align: center; }
  }
  .size-large, .size-tall, .size-small {
    grid-column: span 1;
    grid-row: auto;
    min-height: 120px;
  }
}
</style>
