const fs = require('fs');
const path = 'd:/1a_Projects/A_Project/金桥/260416数据同步/YYCAdmin/YYCAdmin-Vue3/src/views/business/EfMidysbilldata.vue';
let content = fs.readFileSync(path, 'utf8');

const startTag = '<el-dialog :title="title" :lock-scroll="false" v-model="open" width="800px">';
const endTag = '    </el-dialog>';

const startIndex = content.indexOf(startTag);
const endIndex = content.indexOf(endTag, startIndex) + endTag.length;

if (startIndex !== -1 && endIndex !== -1) {
    const newContent = `    <el-dialog :title="title" :lock-scroll="false" v-model="open" width="900px">
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
    </el-dialog>`;
    content = content.substring(0, startIndex) + newContent + content.substring(endIndex);
    fs.writeFileSync(path, content, 'utf8');
    console.log('Dialog content updated successfully.');
} else {
    console.log('Start or end tag not found.');
}
