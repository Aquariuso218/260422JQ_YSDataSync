/* =================================================================
   用友YS自定义报表：金桥资金计划数据报表（测试修正版）
   提示：此版本直接将参数替换为了固定值，方便在 Navicat / DBeaver 中测试数据。
   本版本已根据您调整的代码进行了同步优化，包括：
     1. 将结余计算的自关联条件由“名称关联”修改为更加稳定唯一的“组织编码（org_id_code = manage_accentity_code）”关联。
     2. 将 U8 实际期末余额独立拆分为第 5 级分类：“5-U8资金结余”。
     3. 增加了全局的 ORDER BY `编码`，保证报表在系统输出时维持正确的排序。
     4. 保持无行注释（--）的安全编译规范，防止语义模型引擎解析冲突。
   固定条件：
     1. 期间：2026-07-01 至 2026-07-31
     2. 组织名称：重庆金桥机器制造有限责任公司
   ================================================================= */

WITH 
u8_balance AS (
    SELECT 
        T1.code AS org_id_code,
        T1.name AS org_id_name,
        T0.date_time_0 AS cDate,
        T0.decimal_0 AS OLAmount,   
        T0.decimal_1 AS CLAmount,   
        T0.decimal_2 AS OAAmount,   
        T0.decimal_3 AS CAAmount,
        T0.decimal_4 AS HOLAmount,   
        T0.decimal_5 AS HCLAmount    
    FROM 
        iuap_yonbuilder_dynamic_ds.iuap_yonbuilder_f25303_7_u8tobalance T0 
        LEFT JOIN iuap_apdoc_basedoc.org_orgs T1 ON T1.id = T0.org_id 
    WHERE 
        T0.dr = 0
        AND T1.name = '重庆金桥机器制造有限责任公司'
),
u8_balance_opening AS (
    SELECT 
        u.org_id_code,
        u.org_id_name,
        u.OLAmount,   
        u.OAAmount,
        u.HOLAmount    
    FROM u8_balance u
    WHERE 
        u.cDate = '2026-07-01'
),
u8_balance_closing AS (
    SELECT 
        u.org_id_code,
        u.org_id_name,
        u.CLAmount,   
        u.CAAmount,
        u.HCLAmount    
    FROM u8_balance u
    WHERE 
        u.cDate = '2026-07-01'
),
plan_draw_base AS (
    SELECT
        T0.id AS draw_id,
        T0.CODE AS plan_code,
        T2.id AS manage_accentity_id,
        T2.CODE AS manage_accentity_code,
        T2.NAME AS manage_accentity_name,
        T4.capitalPlanProjectNo AS project_no,
        T4.capitalPlanProjectName AS project_name,
        T4.thisPeriodPlanAmt AS this_period_plan_amt,
        CASE
            WHEN T4.isEnd = TRUE THEN '是'
            ELSE '否'
        END AS is_end
    FROM
        yonbip_fi_ctmfc.cspl_capitalplandraw T0
        LEFT JOIN yonbip_fi_ctmpub.tmsp_org T3 ON T3.id = T0.plan_org AND T3.dr = 0
        LEFT JOIN yonbip_fi_ctmpub.tmsp_org_plan T1 ON T1.id = T3.id
        LEFT JOIN iuap_apdoc_basedoc.org_funds T2 ON T2.id = T1.manage_accentity
        LEFT JOIN yonbip_fi_ctmfc.cspl_capitalplandraw_b T4 ON T4.mainid = T0.id AND T4.iDeleted = 0
    WHERE
        T0.verifystate = 2
        AND T0.iDeleted = 0
        AND T0.beginDate >= '2026-07-01 00:00:00'
        AND T0.endDate <= '2026-07-31 23:59:59'
        AND T2.NAME = '重庆金桥机器制造有限责任公司'
),
plan_detail_summary AS (
    SELECT
        T0.id AS draw_id,
        T3.CODE AS project_code,
        SUM(CASE WHEN T2.cCode IN ('307', '301') THEN T1.amount ELSE 0 END) AS amt_draft,
        SUM(CASE WHEN T2.cCode = '305' THEN T1.amount ELSE 0 END) AS amt_creditor,
        SUM(CASE WHEN T2.cCode NOT IN ('307', '301', '305') OR T2.cCode IS NULL THEN T1.amount ELSE 0 END) AS amt_cash
    FROM
        yonbip_fi_ctmfc.cspl_capitalplandraw T0
        LEFT JOIN yonbip_fi_ctmfc.cspl_capitalplandetail T1 ON T1.mainid = T0.id
        LEFT JOIN iuap_apdoc_coredoc.settle_method T2 ON T2.id = T1.settle_mode
        LEFT JOIN yonbip_fi_ctmfc.cspl_projectset T3 ON T3.id = T1.project_id
    WHERE
        T0.verifystate = 2
        AND T0.iDeleted = 0
    GROUP BY
        T0.id,
        T3.CODE
),
plan_summary_ysmx AS (
    SELECT
        m.manage_accentity_code,
        m.manage_accentity_name,
        m.project_no,
        CONCAT(SUBSTRING_INDEX(m.project_no, '.', 1), '-', COALESCE(p1.project_name, '')) AS project_no1,
        CASE 
            WHEN m.project_no LIKE '%.%' THEN CONCAT(SUBSTRING_INDEX(m.project_no, '.', 2), '-', COALESCE(p2.project_name, '')) 
            ELSE '' 
        END AS project_no2,
        CASE 
            WHEN m.project_no LIKE '%.%.%' THEN CONCAT(SUBSTRING_INDEX(m.project_no, '.', 3), '-', m.project_name) 
            ELSE '' 
        END AS project_no3,
        m.project_name,
        m.is_end,
        SUM(
            CASE 
                WHEN d.project_code IS NULL THEN m.this_period_plan_amt 
                ELSE COALESCE(d.amt_cash, 0) 
            END
        ) AS ykckmoney,
        COALESCE(SUM(d.amt_draft), 0) AS yhhpmoney,
        COALESCE(SUM(d.amt_creditor), 0) AS zjpzmoney,
        SUBSTRING_INDEX(m.project_no, '.', 1) AS first_level_code
    FROM
        plan_draw_base m
        LEFT JOIN plan_draw_base p1 ON p1.draw_id = m.draw_id AND p1.project_no = SUBSTRING_INDEX(m.project_no, '.', 1)
        LEFT JOIN plan_draw_base p2 ON p2.draw_id = m.draw_id AND p2.project_no = SUBSTRING_INDEX(m.project_no, '.', 2)
        LEFT JOIN plan_detail_summary d ON d.draw_id = m.draw_id 
            AND (d.project_code = m.project_no OR d.project_code LIKE CONCAT(m.project_no, '.%'))
    WHERE 
        m.is_end = '是'
    GROUP BY
        m.manage_accentity_code,
        m.manage_accentity_name,
        m.project_no,
        m.project_name,
        m.is_end,
        p1.project_name,
        p2.project_name
),
plan_income_expense_summary AS (
    SELECT
        manage_accentity_code,
        manage_accentity_name,
        SUM(CASE WHEN first_level_code = '2' THEN ykckmoney ELSE 0 END) AS income_cash,
        SUM(CASE WHEN first_level_code = '2' THEN yhhpmoney ELSE 0 END) AS income_draft,
        SUM(CASE WHEN first_level_code = '2' THEN zjpzmoney ELSE 0 END) AS income_creditor,
        SUM(CASE WHEN first_level_code = '3' THEN ykckmoney ELSE 0 END) AS expense_cash,
        SUM(CASE WHEN first_level_code = '3' THEN yhhpmoney ELSE 0 END) AS expense_draft,
        SUM(CASE WHEN first_level_code = '3' THEN zjpzmoney ELSE 0 END) AS expense_creditor
    FROM plan_summary_ysmx
    GROUP BY manage_accentity_code, manage_accentity_name
)
SELECT
    u.org_id_name AS `组织名称`,
    '1.1' AS `编码`,
    '1-期初余额' AS `一级编码`,
    '1.1-其中：锁定金额' AS `二级编码`,
    '' AS `三级编码`,
    '其中：锁定金额' AS `收支项目`,
    '1.1' AS `项目编码`,
    '其中：锁定金额' AS `项目名称`,
    '是' AS `是否末级`,
    u.OLAmount AS `现金/银行存款`,
    u.HOLAmount AS `银行汇票`,
    0.00 AS `债权凭证`
FROM u8_balance_opening u
UNION ALL
SELECT
    u.org_id_name AS `组织名称`,
    '1.2' AS `编码`,
    '1-期初余额' AS `一级编码`,
    '1.2-可支配金额' AS `二级编码`,
    '' AS `三级编码`,
    '可支配金额' AS `收支项目`,
    '1.2' AS `项目编码`,
    '可支配金额' AS `项目名称`,
    '是' AS `是否末级`,
    u.OAAmount AS `现金/银行存款`,
    0.00 AS `银行汇票`,
    0.00 AS `债权凭证`
FROM u8_balance_opening u
UNION ALL
SELECT
    p.manage_accentity_name AS `组织名称`,
    p.project_no AS `编码`,
    p.project_no1 AS `一级编码`,
    p.project_no2 AS `二级编码`,
    p.project_no3 AS `三级编码`,
    p.project_name AS `收支项目`,
    p.project_no AS `项目编码`,
    p.project_name AS `项目名称`,
    p.is_end AS `是否末级`,
    p.ykckmoney AS `现金/银行存款`,
    p.yhhpmoney AS `银行汇票`,
    p.zjpzmoney AS `债权凭证`
FROM plan_summary_ysmx p
WHERE p.first_level_code IN ('2', '3')
UNION ALL
SELECT
    u.org_id_name AS `组织名称`,
    '4.1' AS `编码`,
    '4-资金结余' AS `一级编码`,
    '4.1-其中：锁定金额' AS `二级编码`,
    '' AS `三级编码`,
    '其中：锁定金额' AS `收支项目`,
    '4.1' AS `项目编码`,
    '其中：锁定金额' AS `项目名称`,
    '是' AS `是否末级`,
    u.CLAmount AS `现金/银行存款`,
    u.HCLAmount AS `银行汇票`,
    0 AS `债权凭证`
FROM u8_balance_closing u
UNION ALL
SELECT
    u.org_id_name AS `组织名称`,
    '4.2' AS `编码`,
    '4-资金结余' AS `一级编码`,
    '4.2-可支配金额' AS `二级编码`,
    '' AS `三级编码`,
    '可支配金额' AS `收支项目`,
    '4.2' AS `项目编码`,
    '可支配金额' AS `项目名称`,
    '是' AS `是否末级`,
    u.OAAmount + COALESCE(ies.income_cash, 0) - COALESCE(ies.expense_cash, 0) AS `现金/银行存款`,
    COALESCE(ies.income_draft, 0) - COALESCE(ies.expense_draft, 0) AS `银行汇票`,
    COALESCE(ies.income_creditor, 0) - COALESCE(ies.expense_creditor, 0) AS `债权凭证`
FROM u8_balance_opening u
LEFT JOIN plan_income_expense_summary ies ON u.org_id_code = ies.manage_accentity_code
UNION ALL
SELECT
    u.org_id_name AS `组织名称`,
    '5.1' AS `编码`,
    '5-U8资金结余' AS `一级编码`,
    '5.1-其中:U8锁定金额' AS `二级编码`,
    '' AS `三级编码`,
    'U8锁定金额' AS `收支项目`,
    '5.1' AS `项目编码`,
    'U8锁定金额' AS `项目名称`,
    '是' AS `是否末级`,
    u.CLAmount AS `现金/银行存款`,
    u.HCLAmount AS `银行汇票`,
    0.00 AS `债权凭证`
FROM u8_balance_closing u
UNION ALL
SELECT
    u.org_id_name AS `组织名称`,
    '5.2' AS `编码`,
    '5-U8资金结余' AS `一级编码`,
    '5.2-U8可支配金额' AS `二级编码`,
    '' AS `三级编码`,
    'U8可支配金额' AS `收支项目`,
    '5.2' AS `项目编码`,
    'U8可支配金额' AS `项目名称`,
    '是' AS `是否末级`,
    u.CAAmount AS `现金/银行存款`,
    0.00 AS `银行汇票`,
    0.00 AS `债权凭证`
FROM u8_balance_closing u
ORDER BY `编码`;
