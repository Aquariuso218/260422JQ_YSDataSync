<template>
  <div class="app-container home">
    <noticeBar :dataList="noticeList"> </noticeBar>
    <div class="block text-center">
      <el-carousel height="200px" motion-blur>
        <el-carousel-item v-for="item in 4" :key="item">
          <h3 class="small justify-center" text="2xl">{{ item }}</h3>
        </el-carousel-item>
      </el-carousel>
    </div>
    <el-row :gutter="20">
      <el-col :sm="24" :lg="24">
        <blockquote class="text-warning" style="font-size: 14px">
          <h2>云优创{{ $t('layout.backstageManagement') }}</h2>
          <p>YYCAdmin后台管理系统为企业提供更多定制化开发解决方案。</p>
          <p>
            <b>{{ $t('layout.currentVersion') }}:</b> <span>v{{ version }}</span>
          </p>
        </blockquote>
      </el-col>
    </el-row>
    <el-row :gutter="20">
      <el-col :lg="16" :sm="24">
        <!-- <p></p> -->
        <h3>{{ $t('layout.tip1') }}</h3>
      </el-col>
      <el-col :lg="8" :sm="24">
      </el-col>
    </el-row>
    <el-divider />
    <el-row :gutter="20" class="mt10">
      <el-col :sm="24" :lg="6">
        <el-card>
          <template #header>
            <span>{{ $t('layout.contactUs') }}</span>
          </template>
          <div class="body">
            <p>
              <el-icon> <promotion /> </el-icon>{{ $t('layout.officialWebsite') }}：
              <el-link href="http://www.yonyouc.com" target="_blank"> http://www.yonyouc.com </el-link>
            </p>
            <p>
              <el-icon> <UserFilled /> </el-icon>{{ $t('layout.qqGroup') }}：
            </p>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup name="index">
import noticeBar from '@/components/NoticeBar/index'

const noticeList = ref(['云优创后台管理系统', '官网地址：http://www.yonyouc.com'])

import defaultSettings from '@/settings'
const version = defaultSettings.version

function goTarget(url) {
  window.open(url, '__blank')
}
</script>

<style scoped lang="scss">
.demonstration {
  color: var(--el-text-color-secondary);
}

.el-carousel__item h3 {
  color: #475669;
  opacity: 0.75;
  line-height: 200px;
  margin: 0;
  text-align: center;
}

.el-carousel__item:nth-child(2n) {
  background-color: #99a9bf;
}

.el-carousel__item:nth-child(2n + 1) {
  background-color: #d3dce6;
}

.home {
  blockquote {
    padding: 10px 20px;
    margin: 0 0 20px;
    font-size: 17.5px;
    border-left: 5px solid #eee;
  }

  hr {
    margin-top: 20px;
    margin-bottom: 20px;
    border: 0;
    border-top: 1px solid #eee;
  }

  .col-item {
    margin-bottom: 20px;
  }

  ul {
    padding: 0;
    margin: 0;
  }

  font-family: 'open sans', 'Helvetica Neue', Helvetica, Arial, sans-serif;
  font-size: 13px;
  color: #676a6c;
  overflow-x: hidden;

  ul {
    list-style-type: none;
  }

  h4 {
    margin-top: 0px;
  }

  h2 {
    margin-top: 10px;
    font-size: 26px;
    font-weight: 100;
  }

  p {
    margin-top: 10px;

    b {
      font-weight: 700;
    }
  }

  .update-log {
    ol {
      display: block;
      list-style-type: decimal;
      margin-block-start: 1em;
      margin-block-end: 1em;
      margin-inline-start: 0;
      margin-inline-end: 0;
      padding-inline-start: 40px;
    }
  }
}
</style>
