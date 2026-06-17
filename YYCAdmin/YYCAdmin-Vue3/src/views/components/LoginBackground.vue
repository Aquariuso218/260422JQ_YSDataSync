<template>
  <!-- 
    Liquid Fluid Background — 液态流体背景
    基于不规则 border-radius 的实时形变（morphing）加上缓慢的平移，
    产生水滴/流体般的交融质感。
  -->
  <div class="fluid-root">
    <div class="fluid-base"></div>

    <!-- 容器，可能可加 SVG filter goo 以加强融合感，但原生 CSS mix-blend 性能更好 -->
    <div class="fluid-container">
      <div class="fluid-shape shape-1"></div>
      <div class="fluid-shape shape-2"></div>
      <div class="fluid-shape shape-3"></div>
    </div>

    <!-- 内容层 -->
    <div class="fluid-content">
      <slot />
    </div>

    <!-- 用于加强流体粘滞感的SVG滤镜 (如果需要更强的交融感，可取消注释并给 fluid-container 加 filter: url(#goo))
    <svg width="0" height="0" class="hidden-svg">
      <filter id="goo">
        <feGaussianBlur in="SourceGraphic" stdDeviation="20" result="blur" />
        <feColorMatrix in="blur" mode="matrix" values="1 0 0 0 0  0 1 0 0 0  0 0 1 0 0  0 0 0 30 -10" result="goo" />
      </filter>
    </svg>
    -->
  </div>
</template>

<script setup>
</script>

<style scoped>
.fluid-root {
  position: absolute;
  inset: 0;
  overflow: hidden;
  background: #f0f5ff; /* 清爽的浅蓝底色 */
}

.fluid-base {
  position: absolute;
  inset: 0;
  background: linear-gradient(120deg, #e0ebff 0%, #ffffff 100%);
  z-index: 0;
}

.fluid-container {
  position: absolute;
  inset: 0;
  z-index: 1;
  /* 如果希望流体之间有极强的水滴融合感，可打开这行和SVG： */
  /* filter: url(#goo); */
}

/* 核心流体块基础样式 */
.fluid-shape {
  position: absolute;
  filter: blur(20px);
  mix-blend-mode: multiply; /* 在浅色底上使用正片叠底产生深度交叉 */
  will-change: transform, border-radius;
}

/* 形状1：科技蓝主体 */
.shape-1 {
  width: 600px;
  height: 600px;
  top: -100px;
  left: -100px;
  background: linear-gradient(135deg, #0052d9 0%, #4facfe 100%);
  opacity: 0.6;
  animation: liquid-morph-1 12s ease-in-out infinite;
}

/* 形状2：亮青色穿插 */
.shape-2 {
  width: 500px;
  height: 500px;
  bottom: -100px;
  right: -50px;
  background: linear-gradient(135deg, #00f2fe 0%, #4facfe 100%);
  opacity: 0.5;
  animation: liquid-morph-2 15s ease-in-out infinite alternate;
}

/* 形状3：柔和紫点缀 */
.shape-3 {
  width: 450px;
  height: 450px;
  top: 30%;
  left: 40%;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  opacity: 0.4;
  animation: liquid-morph-3 18s ease-in-out infinite;
}

/* 液态流体形变动画 */
@keyframes liquid-morph-1 {
  0% {
    transform: translate(0, 0) rotate(0deg) scale(1);
    border-radius: 60% 40% 30% 70% / 60% 30% 70% 40%;
  }
  33% {
    transform: translate(15%, 5%) rotate(45deg) scale(1.05);
    border-radius: 30% 60% 70% 40% / 50% 60% 30% 60%;
  }
  66% {
    transform: translate(-10%, 15%) rotate(90deg) scale(0.95);
    border-radius: 50% 50% 40% 60% / 30% 60% 40% 70%;
  }
  100% {
    transform: translate(0, 0) rotate(180deg) scale(1);
    border-radius: 60% 40% 30% 70% / 60% 30% 70% 40%;
  }
}

@keyframes liquid-morph-2 {
  0% {
    transform: translate(0, 0) rotate(0deg) scale(1);
    border-radius: 40% 60% 60% 40% / 40% 30% 70% 60%;
  }
  50% {
    transform: translate(-15%, -15%) rotate(-45deg) scale(1.1);
    border-radius: 60% 40% 30% 70% / 60% 50% 40% 40%;
  }
  100% {
    transform: translate(0, 0) rotate(-90deg) scale(1);
    border-radius: 40% 60% 60% 40% / 40% 30% 70% 60%;
  }
}

@keyframes liquid-morph-3 {
  0% {
    transform: translate(0, 0) rotate(0deg);
    border-radius: 50% 50% 30% 70% / 50% 70% 30% 50%;
  }
  50% {
    transform: translate(-10%, -20%) rotate(45deg);
    border-radius: 70% 30% 50% 50% / 30% 50% 70% 50%;
  }
  100% {
    transform: translate(0, 0) rotate(90deg);
    border-radius: 50% 50% 30% 70% / 50% 70% 30% 50%;
  }
}

.fluid-content {
  position: relative;
  z-index: 2;
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  justify-content: center;
  /* 左侧加一点浅色遮罩，增加深度 */
  background: linear-gradient(90deg, rgba(0, 30, 80, 0.05) 0%, transparent 100%);
}

/* 隐藏滤镜用 svg */
.hidden-svg {
  position: absolute;
  width: 0;
  height: 0;
  pointer-events: none;
}
</style>
