# ERPNext 对照设计：采购、收货与库存

版本 0.1，2026-11-09。适用于 `LearningErp.Api/Erp`。以 ERPNext 官方操作文档及 `version-16` 分支源码为参考，先研究一条采购流程，再在现有 C#、ASP.NET Core、EF Core 和 SQLite 项目中实现。本文的开发顺序是本项目的安排。

## 1. 用一张单理解采购与收货

向 `SUP-001` 订购 `PCB-001` 主板 10 块，今天到 6 块，明天到 4 块。需要保存一张采购单和两张收货单。两个收货明细都引用同一条采购明细。第一天采购进度为 6/10，第二天为 10/10。如果仓库此前没有该物料，库存分别为 6 和 10，并能通过两条库存流水回查对应收货单。创建采购草稿、提交和审批均不增加实物库存。

ERPNext 的采购文档展示了从已提交采购单创建收货单的路径，也允许根据配置在特定情况下直接收货。提交收货会记录库存流水并更新关联采购单的已收进度，支持一张采购单对应多次收货。[采购单](https://docs.frappe.io/erpnext/purchase-order) · [收货单](https://docs.frappe.io/erpnext/purchase-receipt)

## 2. 同一功能在两套技术里的位置

| 要解决的问题 | ERPNext 参考 | 本项目的落点 |
| --- | --- | --- |
| 单据字段和一对多关系 | `purchase_order.json` 中的单据定义及采购明细子表 | `PurchaseOrder` 和 `PurchaseOrderLine`，由 `ErpDbContext` 配置外键 |
| 页面交互 | `purchase_order.js` 和 `purchase_order_list.js` | `purchase-orders.html` 及其 JavaScript，调用 ASP.NET Core 的 API |
| 保存前校验 | `purchase_order.py` 的 `validate()`，检查供应商、交期、物料等 | `PurchaseOrderService.Create()` 检查供应商、物料启用状态和数量，提交阶段按需要重新校验 |
| 提交后动作 | Frappe 的 `on_submit()` 等生命周期方法，采购单提交后更新关联单据和订购数量 | Controller 提供操作接口，Service 决定是否允许状态转换以及需要写入哪些记录 |
| 收货追溯到采购明细 | 收货明细含 `purchase_order`、`purchase_order_item`，对应来源单与来源行 | `PurchaseReceipt.PurchaseOrderId` 与 `PurchaseReceiptLine.PurchaseOrderLineId`，后端校验来源行属于该单 |
| 规则验证 | 采购单目录中有 `test_purchase_order.py` | 先在浏览器用正常和异常场景验收，关键收货规则再补集成测试 |

源码入口：[采购单模块](https://github.com/frappe/erpnext/tree/version-16/erpnext/buying/doctype/purchase_order) · [采购单服务端逻辑](https://github.com/frappe/erpnext/blob/version-16/erpnext/buying/doctype/purchase_order/purchase_order.py) · [收货明细字段](https://github.com/frappe/erpnext/blob/version-16/erpnext/stock/doctype/purchase_receipt_item/purchase_receipt_item.json) · [Frappe 生命周期](https://docs.frappe.io/framework/user/en/basics/doctypes/controllers)。Frappe 的 Controller 是单据行为类，和 ASP.NET Core 的 HTTP Controller 职责不同。

## 3. 本项目的采购规则

| 编号 | 规则 | 验收 |
| --- | --- | --- |
| PUR-01 | 采购单必须关联已有供应商；每条明细必须是启用物料，数量大于零 | 创建含停用物料的草稿返回 400，数据库无新单 |
| PUR-02 | 本项目使用 `Draft → Submitted → Approved`。`Submitted` 表示待审批，`Approved` 才允许收货。这是模拟流程的定义 | 草稿直接审批、已批准单再次提交均返回 409，状态保持原值 |
| PUR-03 | 收货必须引用已批准的采购单。收货数量为正整数，累计量不得超过对应采购行订购量 | 订购 10，已收 6，新增收货 5 返回 409；采购进度和库存不变 |
| PUR-04 | 每次收货生成独立单据，库存流水关联对应收货行。首版收货按合格入库模拟 | 收 6 再收 4，采购详情显示剩余量依次为 4 和 0；两条流水合计为 10 |
| PUR-05 | 收货单、收货明细、库存流水和余额在同一数据库事务中保存；校验与写入要防止并发超收 | 同一请求重试不会重复增加库存；两人同时争抢最后数量时，累计量不超订购量 |

ERPNext 支持的独立收货、超收容差、合格与不合格仓库、采购发票和库存估值留待后续专题。当前只练习按单收货、库存数量和来源追溯。采购审批状态与收货进度分开：已批准的单仍可处于未收、部分收或全部收，进度由有效收货明细汇总。

## 4. 按小步开发

| 阶段 | 页面、数据与接口 | 本步验收 |
| --- | --- | --- |
| E01 采购列表 | 先完成 `GET /api/purchase-orders`，只返回 ID、供应商、状态、行数；再接页面列表，点击复用现有详情 API | 列表显示已有草稿，创建成功后刷新，详情行数和摘要一致 |
| E02 采购提交与审批 | 在详情页加提交与审批按钮；拟新增 `POST /api/purchase-orders/{id}/submit` 与 `POST /api/purchase-orders/{id}/approve`，由 Service 控制状态 | 合法流程能走完，非法跳转返回 409。先模拟审批动作，账户权限以后加入 |
| E03a 收货设计与建表 | 设计仓库、收货单和收货明细。单头关联采购单和仓库；每条收货行关联采购明细 | 迁移后能从收货明细找到原采购行，能解释为何不能仅用物料编码判断收了哪一行 |
| E03b 收货与库存入账 | 设计 `POST /api/purchase-orders/{id}/receipts`；在一个事务内校验、保存收货、写流水和更新余额。库存键暂定仓库加物料 | 验证 6 + 4、超收、重复请求及并发。采购详情能查已收和剩余数量 |

每做完一步，再开始下一步的实现。当前已验证采购草稿创建与按 ID 查询；采购列表 DTO 已存在，服务查询尚未写完，控制器尚未映射列表 GET。列表只读现有表，无需迁移。页面接入和状态变更按上表继续。

后续 BOM、生产工单和库存也沿用同一阅读方法：先跑通或阅读产品操作，再找单据字段和服务端规则，最后对照源码中的测试，写成适合本项目的验收场景。全模块顺序见 [模块设计与实施路线](09-module-design-roadmap.md)。
