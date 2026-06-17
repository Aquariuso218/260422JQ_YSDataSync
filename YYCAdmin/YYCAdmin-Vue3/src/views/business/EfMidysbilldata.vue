<!--
 * @Descripttion: (/EF_MidYSBillData)
 * @Author: (admin)
 * @Date: (2026-04-29)
-->
<template>
  <div>
    <el-form :model="queryParams" label-width="100px" inline ref="queryRef" v-show="showSearch" @submit.prevent>
      <el-form-item label="子表ID" prop="id">
        <el-input v-model="queryParams.id" placeholder="请输入子表ID" clearable style="width: 200px" />
      </el-form-item>
      <el-form-item label="表头ID" prop="mainId">
        <el-input v-model="queryParams.mainId" placeholder="请输入表头ID" clearable style="width: 200px" />
      </el-form-item>
      <el-form-item label="单据编码" prop="cVouchCode">
        <el-input v-model="queryParams.cVouchCode" placeholder="请输入单据编码" clearable style="width: 200px" />
      </el-form-item>
      <el-form-item label="单据日期">
        <el-date-picker v-model="dateRangeBillDate" type="daterange" range-separator="至" start-placeholder="开始日期"
          end-placeholder="结束日期" value-format="YYYY-MM-DD HH:mm:ss" style="width: 240px"></el-date-picker>
      </el-form-item>
      <el-form-item label="结算状态" prop="settleStatus">
        <el-input v-model="queryParams.settleStatus" placeholder="请输入结算状态" clearable style="width: 200px" />
      </el-form-item>
      <el-form-item label="款项类型名称" prop="quickTypeName">
        <el-input v-model="queryParams.quickTypeName" placeholder="请输入款项类型" clearable style="width: 200px" />
      </el-form-item>
      <el-form-item label="单据类型" prop="cVouchType">
        <el-input v-model="queryParams.cVouchType" placeholder="请输入单据类型" clearable style="width: 200px" />
      </el-form-item>
      <el-form-item label="对象类型" prop="cDwType">
        <el-input v-model="queryParams.cDwType" placeholder="请输入对象类型" clearable style="width: 200px" />
      </el-form-item>
      <el-form-item label="处理状态" prop="processStatus">
        <el-select v-model="queryParams.processStatus" placeholder="请选择处理状态" clearable style="width: 200px">
          <el-option v-for="item in options.processStatusOptions" :key="item.dictValue" :label="item.dictLabel"
            :value="item.dictValue"></el-option>
        </el-select>
      </el-form-item>
      <el-form-item label="U8单据号" prop="u8Code">
        <el-input v-model="queryParams.u8Code" placeholder="请输入U8单据号" clearable style="width: 200px" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" icon="search" @click="handleQuery">{{ $t('btn.search') }}</el-button>
        <el-button icon="refresh" @click="resetQuery">{{ $t('btn.reset') }}</el-button>
      </el-form-item>
    </el-form>
    <!-- 工具区域 -->
    <el-row :gutter="15" class="mb10">
      <!-- <el-col :span="1.5">
        <el-button type="primary" v-hasPermi="['efmidysbilldata:add']" plain icon="plus" @click="handleAdd">
          {{ $t('btn.add') }}
        </el-button>
      </el-col> -->
      <right-toolbar v-model:showSearch="showSearch" @queryTable="getList" :columns="columns"></right-toolbar>
    </el-row>

    <el-table :data="dataList" v-loading="loading" ref="table" border header-cell-class-name="el-table-header-cell"
      highlight-current-row @sort-change="sortChange">
      <el-table-column prop="autoId" label="自增主键" align="center" min-width="100" v-if="columns.showColumn('autoId')" />
      <el-table-column prop="id" label="子表ID" align="center" :show-overflow-tooltip="true" min-width="180"
        v-if="columns.showColumn('id')" />
      <el-table-column prop="mainId" label="表头ID" align="center" :show-overflow-tooltip="true" min-width="180"
        v-if="columns.showColumn('mainId')" />
      <el-table-column prop="cVouchCode" label="单据编码" align="center" :show-overflow-tooltip="true" min-width="150"
        v-if="columns.showColumn('cVouchCode')" />
      <el-table-column prop="billDate" label="单据日期" :show-overflow-tooltip="true" min-width="150"
        v-if="columns.showColumn('billDate')" />
      <el-table-column prop="cMaker" label="制单人名" align="center" :show-overflow-tooltip="true" min-width="120"
        v-if="columns.showColumn('cMaker')" />
      <el-table-column prop="orgCode" label="组织编码" align="center" :show-overflow-tooltip="true" min-width="120"
        v-if="columns.showColumn('orgCode')" />
      <el-table-column prop="cDepCode" label="部门编码" align="center" :show-overflow-tooltip="true" min-width="120"
        v-if="columns.showColumn('cDepCode')" />
      <el-table-column prop="cNatBankAccount" label="企业单位银行账号" align="center" :show-overflow-tooltip="true"
        min-width="180" v-if="columns.showColumn('cNatBankAccount')" />
      <el-table-column prop="cNatBank" label="企业单位银行账户名称" align="center" :show-overflow-tooltip="true" min-width="200"
        v-if="columns.showColumn('cNatBank')" />
      <el-table-column prop="cssName" label="结算方式名称" align="center" :show-overflow-tooltip="true" min-width="150"
        v-if="columns.showColumn('cssName')" />
      <el-table-column prop="settleStatus" label="结算状态" align="center" min-width="100"
        v-if="columns.showColumn('settleStatus')">
        <template #default="scope">
          <el-tag :type="scope.row.settleStatus == 3 ? 'success' : 'info'">
            {{ scope.row.settleStatus == 3 ? '已结算' : '未结算' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="quickTypeName" label="款项类型名称" align="center" :show-overflow-tooltip="true" min-width="150"
        v-if="columns.showColumn('quickTypeName')" />
      <el-table-column prop="cVouchType" label="单据类型" align="center" :show-overflow-tooltip="true" min-width="150"
        v-if="columns.showColumn('cVouchType')" />
      <el-table-column prop="cDwType" label="对象类型" align="center" :show-overflow-tooltip="true" min-width="150"
        v-if="columns.showColumn('cDwType')" />
      <el-table-column prop="cDwCode" label="对象编码" align="center" :show-overflow-tooltip="true" min-width="150"
        v-if="columns.showColumn('cDwCode')" />
      <el-table-column prop="iAmount" label="金额" align="center" min-width="120" v-if="columns.showColumn('iAmount')" />
      <el-table-column prop="cNoteCode" label="票据号" align="center" :show-overflow-tooltip="true" min-width="180"
        v-if="columns.showColumn('cNoteCode')" />
      <el-table-column prop="tradetypeName" label="来源交易类型" align="center" :show-overflow-tooltip="true" min-width="150"
        v-if="columns.showColumn('tradetypeName')" />
      <el-table-column prop="discountInterest" label="利息" align="center" min-width="120"
        v-if="columns.showColumn('discountInterest')" />
      <el-table-column prop="noteTypeCode" label="票据类型" align="center" :show-overflow-tooltip="true" min-width="150"
        v-if="columns.showColumn('noteTypeCode')" />
      <el-table-column prop="createTime" label="数据写入时间" :show-overflow-tooltip="true" min-width="180"
        v-if="columns.showColumn('createTime')" />
      <el-table-column prop="updateTime" label="状态刷新时间" :show-overflow-tooltip="true" min-width="180"
        v-if="columns.showColumn('updateTime')" />
      <el-table-column prop="processStatus" label="处理状态" align="center" min-width="100"
        v-if="columns.showColumn('processStatus')">
        <template #default="scope">
          <dict-tag :options="options.processStatusOptions" :value="scope.row.processStatus" />
        </template>
      </el-table-column>
      <el-table-column prop="processMsg" label="报错信息" align="center" :show-overflow-tooltip="true" min-width="200"
        v-if="columns.showColumn('processMsg')" />
      <el-table-column prop="u8Code" label="U8单据号" align="center" :show-overflow-tooltip="true" min-width="150"
        v-if="columns.showColumn('u8Code')" />
      <el-table-column prop="synTime" label="写入U8时间" :show-overflow-tooltip="true" min-width="180"
        v-if="columns.showColumn('synTime')" />
      <el-table-column label="操作" width="100" fixed="right">
        <template #default="scope">
          <el-button type="primary" size="small" icon="view" title="查看" @click="handleView(scope.row)">查看</el-button>
        </template>
      </el-table-column>
    </el-table>
    <pagination :total="total" v-model:page="queryParams.pageNum" v-model:limit="queryParams.pageSize"
      @pagination="getList" />


    <el-dialog :title="title" :lock-scroll="false" v-model="open" width="900px">
      <el-descriptions :column="2" border>
        <el-descriptions-item label="自增主键">{{ form.autoId }}</el-descriptions-item>
        <el-descriptions-item label="子表ID">{{ form.id }}</el-descriptions-item>
        <el-descriptions-item label="表头ID">{{ form.mainId }}</el-descriptions-item>
        <el-descriptions-item label="单据编码">{{ form.cVouchCode }}</el-descriptions-item>
        <el-descriptions-item label="单据日期">{{ form.billDate }}</el-descriptions-item>
        <el-descriptions-item label="制单人名">{{ form.cMaker }}</el-descriptions-item>
        <el-descriptions-item label="组织编码">{{ form.orgCode }}</el-descriptions-item>
        <el-descriptions-item label="部门编码">{{ form.cDepCode }}</el-descriptions-item>
        <el-descriptions-item label="企业单位银行账号">{{ form.cNatBankAccount }}</el-descriptions-item>
        <el-descriptions-item label="企业单位银行账户名称">{{ form.cNatBank }}</el-descriptions-item>
        <el-descriptions-item label="结算方式名称">{{ form.cssName }}</el-descriptions-item>
        <el-descriptions-item label="结算状态">
          <el-tag :type="form.settleStatus == 3 ? 'success' : 'info'">
            {{ form.settleStatus == 3 ? '已结算' : '未结算' }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="款项类型名称">{{ form.quickTypeName }}</el-descriptions-item>
        <el-descriptions-item label="单据类型">{{ form.cVouchType }}</el-descriptions-item>
        <el-descriptions-item label="对象类型">{{ form.cDwType }}</el-descriptions-item>
        <el-descriptions-item label="对象编码">{{ form.cDwCode }}</el-descriptions-item>
        <el-descriptions-item label="金额">{{ form.iAmount }}</el-descriptions-item>
        <el-descriptions-item label="票据号">{{ form.cNoteCode }}</el-descriptions-item>
        <el-descriptions-item label="来源交易类型">{{ form.tradetypeName }}</el-descriptions-item>
        <el-descriptions-item label="利息">{{ form.discountInterest }}</el-descriptions-item>
        <el-descriptions-item label="票据类型">{{ form.noteTypeCode }}</el-descriptions-item>
        <el-descriptions-item label="数据写入时间">{{ form.createTime }}</el-descriptions-item>
        <el-descriptions-item label="状态刷新时间">{{ form.updateTime }}</el-descriptions-item>
        <el-descriptions-item label="处理状态">
          <dict-tag :options="options.processStatusOptions" :value="form.processStatus" />
        </el-descriptions-item>
        <el-descriptions-item label="报错信息">{{ form.processMsg }}</el-descriptions-item>
        <el-descriptions-item label="U8单据号">{{ form.u8Code }}</el-descriptions-item>
        <el-descriptions-item label="写入U8时间">{{ form.synTime }}</el-descriptions-item>
      </el-descriptions>
      <template #footer v-if="opertype != 3">
        <el-button text @click="cancel">{{ $t('btn.cancel') }}</el-button>
        <el-button type="primary" @click="submitForm">{{ $t('btn.submit') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup name="efmidysbilldata">
import {
  listEfMidysbilldata,
  addEfMidysbilldata, delEfMidysbilldata,
  updateEfMidysbilldata, getEfMidysbilldata,
}
  from '@/api/business/efmidysbilldata.js'
const { proxy } = getCurrentInstance()
const ids = ref([])
const loading = ref(false)
const showSearch = ref(true)
const dateRangeBillDate = ref([])
const queryParams = reactive({
  pageNum: 1,
  pageSize: 10,
  sort: '',
  sortType: 'asc',
  id: undefined,
  mainId: undefined,
  cVouchCode: undefined,
  settleStatus: undefined,
  quickTypeName: undefined,
  cVouchType: undefined,
  cDwType: undefined,
  processStatus: undefined,
  u8Code: undefined,
})
const columns = ref([
  { visible: true, align: 'center', type: '', prop: 'autoId', label: '自增主键' },
  { visible: true, align: 'center', type: '', prop: 'id', label: '子表ID', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'mainId', label: '表头ID', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'cVouchCode', label: '单据编码', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'billDate', label: '单据日期', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'cMaker', label: '制单人名', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'orgCode', label: '组织编码', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'cDepCode', label: '部门编码', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'cNatBankAccount', label: '企业单位银行账号', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'cNatBank', label: '企业单位银行账户名称', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'cssName', label: '结算方式名称', showOverflowTooltip: true },
  { visible: true, align: 'center', type: 'dict', prop: 'settleStatus', label: '结算状态' },
  { visible: true, align: 'center', type: '', prop: 'quickTypeName', label: '款项类型名称', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'cVouchType', label: '单据类型', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'cDwType', label: '对象类型', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'cDwCode', label: '对象编码', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'iAmount', label: '金额' },
  { visible: true, align: 'center', type: '', prop: 'cNoteCode', label: '票据号', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'tradetypeName', label: '来源交易类型', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'discountInterest', label: '利息' },
  { visible: true, align: 'center', type: '', prop: 'noteTypeCode', label: '票据类型', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'createTime', label: '数据写入时间', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'updateTime', label: '状态刷新时间', showOverflowTooltip: true },
  { visible: true, align: 'center', type: 'dict', prop: 'processStatus', label: '处理状态' },
  { visible: true, align: 'center', type: '', prop: 'processMsg', label: '报错信息', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'u8Code', label: 'U8单据号', showOverflowTooltip: true },
  { visible: true, align: 'center', type: '', prop: 'synTime', label: '写入U8时间', showOverflowTooltip: true },
  //{ visible: false, prop: 'actions', label: '操作', type: 'slot', width: '160' }
])
const total = ref(0)
const dataList = ref([])
const queryRef = ref()
const defaultTime = ref([new Date(2000, 1, 1, 0, 0, 0), new Date(2000, 2, 1, 23, 59, 59)])


var dictParams = [
]


function getList() {
  loading.value = true
  listEfMidysbilldata(proxy.addDateRange(queryParams, dateRangeBillDate.value, 'BillDate')).then(res => {
    const { code, data } = res
    if (code == 200) {
      dataList.value = data.result
      total.value = data.totalNum
      loading.value = false
    }
  })
}

// 查询
function handleQuery() {
  queryParams.pageNum = 1
  getList()
}

// 重置查询操作
function resetQuery() {
  dateRangeBillDate.value = []
  proxy.resetForm("queryRef")
  handleQuery()
}
// 自定义排序
function sortChange(column) {
  var sort = undefined
  var sortType = undefined

  if (column.prop != null && column.order != null) {
    sort = column.prop
    sortType = column.order

  }
  queryParams.sort = sort
  queryParams.sortType = sortType
  handleQuery()
}

/*************** form操作 ***************/
const formRef = ref()
const title = ref('')
// 操作类型 1、add 2、edit 3、view
const opertype = ref(0)
const open = ref(false)
const state = reactive({
  single: true,
  multiple: true,
  form: {},
  rules: {
  },
  options: {
    // Settlestatus 选项列表 格式 eg:{ dictLabel: '标签', dictValue: '0'}
    settlestatusOptions: [],
    processStatusOptions: [
      { dictLabel: '未处理', dictValue: '0', listClass: 'info' },
      { dictLabel: '成功', dictValue: '1', listClass: 'success' },
      { dictLabel: '失败', dictValue: '2', listClass: 'danger' }
    ],
    cVouchTypeOptions: [],
    cDwTypeOptions: []
  }
})

const { form, rules, options, single, multiple } = toRefs(state)

// 关闭dialog
function cancel() {
  open.value = false
  reset()
}

// 重置表单
function reset() {
  form.value = {
    autoId: null,
    id: null,
    mainId: null,
    cVouchCode: null,
    billDate: null,
    cMaker: null,
    orgCode: null,
    cDepCode: null,
    cNatBankAccount: null,
    cNatBank: null,
    cssName: null,
    settleStatus: null,
    quickTypeName: null,
    cVouchType: null,
    cDwType: null,
    cDwCode: null,
    iAmount: null,
    cNoteCode: null,
    tradetypeName: null,
    discountInterest: null,
    noteTypeCode: null,
    createTime: null,
    updateTime: null,
    processStatus: null,
    processMsg: null,
    u8Code: null,
    synTime: null,
  };
  proxy.resetForm("formRef")
}


// 添加按钮操作
function handleAdd() {
  reset();
  open.value = true
  title.value = '添加'
  opertype.value = 1
}
// 查看按钮操作
function handleView(row) {
  reset()
  const id = row.autoId || ids.value
  getEfMidysbilldata(id).then((res) => {
    const { code, data } = res
    if (code == 200) {
      open.value = true
      title.value = '查看详情'
      opertype.value = 3

      form.value = {
        ...data,
      }
    }
  })
}

// 修改按钮操作
function handleUpdate(row) {
  reset()
  const id = row.autoId || ids.value
  getEfMidysbilldata(id).then((res) => {
    const { code, data } = res
    if (code == 200) {
      open.value = true
      title.value = '修改'
      opertype.value = 2

      form.value = {
        ...data,
      }
    }
  })
}

// 添加&修改 表单提交
function submitForm() {
  proxy.$refs["formRef"].validate((valid) => {
    if (valid) {

      if (form.value.autoId != undefined && opertype.value === 2) {
        updateEfMidysbilldata(form.value).then((res) => {
          proxy.$modal.msgSuccess("修改成功")
          open.value = false
          getList()
        })
      } else {
        addEfMidysbilldata(form.value).then((res) => {
          proxy.$modal.msgSuccess("新增成功")
          open.value = false
          getList()
        })
      }
    }
  })
}

// 删除按钮操作
function handleDelete(row) {
  const Ids = row.autoId || ids.value

  proxy
    .$confirm('是否确认删除参数编号为"' + Ids + '"的数据项？', "警告", {
      confirmButtonText: proxy.$t('common.ok'),
      cancelButtonText: proxy.$t('common.cancel'),
      type: "warning",
    })
    .then(function () {
      return delEfMidysbilldata(Ids)
    })
    .then(() => {
      getList()
      proxy.$modal.msgSuccess("删除成功")
    })
}




handleQuery()
</script>