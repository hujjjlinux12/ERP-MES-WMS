# ERP 模块详细设计

版本 1.0。以下是本项目的拟建规格，已有能力见 [总体设计](11-erp-design-index.md)。接口省略 `/api` 前缀。列表查询默认返回摘要，详情包含明细；每段验收都可以用工作台或接口验证。

## M01 工作台、用户与审计

- **角色和功能**：管理员管理账号与角色，采购员建单和提交，审批员审批，仓管收发与盘点，生产员报工，质检员放行，销售员维护销售单，财务员处理模拟对账。
- **数据**：`User`、`Role`、`UserRole`、`AuditLog`。日志关联业务实体类型、ID、动作、前后状态与操作者；使用服务端认证框架提供的用户标识。
- **页面与接口**：统一导航、我的待办、用户与角色、审计查询。`/users`、`/roles`、`/audit-logs`。认证接入时使用 ASP.NET Core 的认证和授权机制。
- **规则与验收**：权限由服务端判定。以仓管身份直接调用采购审批应返回 403；以审批员操作后能查到操作者和状态变化。

## M02 基础资料

- **功能**：物料、单位、供应商、客户、仓库。物料分类支持原料、半成品、成品；关键件可标记批次或 SN 管理要求。
- **数据**：`Material(Code, Name, Unit, IsActive, ItemType)`、`Supplier`、`Customer`、`Warehouse`；按需要扩展 `Location` 和单位换算，当前数量使用物料基本单位。
- **页面与接口**：资料列表、筛选、详情、维护及启停。复用 `/materials`、`/suppliers`，逐步加 `/customers`、`/warehouses`；启停延续物料 `PUT /materials/{code}/status`。
- **规则与验收**：编码唯一；重复提交返回 409。已引用物料不能直接删除，返回可理解的冲突说明。停用 `PCB-001` 后历史采购单仍可查看，新采购单不能使用。

## M03 采购与供应商交付

- **功能**：采购草稿、列表详情、提交、审批、分批收货、采购进度、关闭和退货。采购申请、询价与比价作为后续增量。
- **数据**：`PurchaseOrder(Id, OrderNo, SupplierCode, Status, CreatedAt)`；`PurchaseOrderLine(Id, PurchaseOrderId, MaterialCode, Quantity, UnitPrice)`；`PurchaseReceipt` 关联采购单和仓库；`PurchaseReceiptLine` 关联采购明细，存本次收货数量。退货行引用原收货明细。金额与业务单号在后续迁移加入。
- **页面与接口**：采购列表与详情、草稿表单、审批按钮、收货单与进度。`GET/POST /purchase-orders`、`GET /purchase-orders/{id}`、`POST /purchase-orders/{id}/submit`、`/approve`、`/receipts`。
- **规则与验收**：`Draft → Submitted → Approved`；只有已批准订单能收货，累计收货按采购行计算。订 10 收 6 再尝试收 5 应拒绝，改收 4 应完成。退货减少对应仓库库存，原收货和反向流水均可查。是否重新开放采购未收数量，在退货步骤实现前明确。本项目的审批状态见 [采购对照设计](10-erpnext-procurement-reference.md)。

## M04 库存、库位与盘点

- **功能**：库存查询、来源流水、移库、盘点和调整，后续引入批次、待检和冻结。
- **数据**：`StockBalance(WarehouseCode, MaterialCode, OnHand)`、`StockLedger(Id, WarehouseCode, MaterialCode, Delta, SourceType, SourceId, SourceLineId, PostedAt)`、`StockTransfer`、`StockCount` 及明细。余额用仓库加物料唯一，流水保留单据来源。扩展批次时同步调整库存键和查询汇总。
- **页面与接口**：余额表、流水追溯、移库及盘点表单。`/stock-balances`、`/stock-ledger`、`/stock-transfers`、`/stock-counts`。
- **规则与验收**：移库在一个事务中写一出一入，净变化为零；库存不足拒绝。盘点记录账面量、实盘量与差异，经确认后用调整流水入账。若盘点期间余额变化，重新核对再过账。示例：原料仓有 10，转移 3 后为 7 和 3，流水能解释两边余额。[ERPNext 盘点](https://docs.frappe.io/erpnext/stock-reconciliation)

## M05 BOM 与生产计划

- **功能**：多层 BOM、版本、发布、用量展开和简单缺料计算。显示器 BOM 引用板卡与面板，板卡 BOM 引用 PCB 和电子料。
- **数据**：`BomVersion(Id, ProductCode, Version, Status, EffectiveFrom)`、`BomLine(ComponentCode, QuantityPer, ChildBomVersionId)`；用量展开结果可供工单保存快照。
- **页面与接口**：BOM 列表、编辑、展开预览和发布。`/boms`、`/boms/{id}/release`、`/boms/{id}/explosion?quantity=10`。
- **规则与验收**：发布前检查组件存在、用量为正、无循环引用。生产 10 台，每台用 1 块板卡，应展开为 10 块板卡，继续展开可看其原料需求。发布新版本不改变已下达工单。首版缺料计算基于当前可用数量，MRP 扩展再考虑未交采购、交期和预留。[ERPNext BOM](https://docs.frappe.io/erpnext/bill-of-materials)

## M06 生产工单与领退料

- **功能**：工单下达、备料、领料、退料、分批完工、成品入库和关闭。工位操作由 MES 衔接。
- **数据**：`WorkOrder(Id, ProductCode, BomVersionId, PlannedQuantity, Status)`、`WorkOrderMaterial` 保存需料快照；`MaterialIssue`、`MaterialReturn`、`ProductionReceipt` 及明细引用工单。
- **页面与接口**：工单详情展示计划、领料、完工和欠量。`/work-orders`、`/work-orders/{id}/release`、`/issues`、`/returns`、`/completions`。
- **规则与验收**：草稿不领料；超可用库存拒绝；退料不超该工单净领料。若先发往线边库，用转移流水，实际消耗另记，避免重复扣减。完工入库按工单来源增加库存，接入品质后须有放行结果。工单计划 10，分批完工 6 和 4，余额和完工量同时正确。[ERPNext 工单](https://docs.frappe.io/erpnext/work-order)

## M07 MES、SMT 和品质接口

- **功能**：接收已下达工单、记录料卷上料与板卡 SN、整机过站、测试、返修及品质放行。ERP 保留工单、数量与库存单据，MES 保留工位过程和单件证据。
- **数据**：`OperationRecord`、`Reel`、`MaterialLoadingRecord`、`BoardSerial`、`UnitSerial`、`BoardUnitBinding`、`Inspection`、`Repair`。绑定要保留拆换历史。
- **页面与接口**：上料防错、过站、检验、返修和追溯页面。拟 `/work-orders/{id}/operations`、`/inspections`、`/traceability/units/{serial}`。
- **规则与验收**：同一报工请求重复提交不计双份；不合格件隔离，复检放行后才可入库。选一块板卡验证料卷、上料、板卡、整机链路。只记录工单发料时展示候选影响范围。细节见 [SMT 设计](07-smt-requirements.md) 和 [ERPNext 品质检验参考](https://docs.frappe.io/erpnext/quality-inspection)。

## M08 销售、包装与出货

- **功能**：客户下单、确认、分批出货、包装及销售退货。报价和复杂定价后续扩展。
- **数据**：`SalesOrder`、`SalesOrderLine`、`Shipment`、`ShipmentLine`、`Package` 和 `SalesReturn`。出货行关联销售明细；启用序列号管理时关联有效整机和包装。
- **页面与接口**：销售单列表、详情、待发数量、出货复核和退货。`/sales-orders`、`/sales-orders/{id}/confirm`、`/shipments`、`/sales-returns`。
- **规则与验收**：销售单确认只形成业务承诺；实际出货才扣库存。累计出货不超订单且不能扣成负数。每台整机只能有一次有效出货；退货关联原出货行并记录验收去向。确认 10 台、出货 6 台，详情显示剩余 4 台且库存减少 6。[ERPNext 销售订单](https://docs.frappe.io/erpnext/sales-order) · [出货单](https://docs.frappe.io/erpnext/delivery-note)

## M09 模拟财务、对账与成本

- **功能**：先实现采购应付、销售应收、分次付款和收款；后续加入总账、会计期间和成本演示。数量库存和财务余额各有独立的流水。
- **数据**：`PurchaseInvoice`、`SalesInvoice` 及来源明细；`Payment`、`PaymentAllocation`；后续 `Account`、`JournalEntry`、`JournalLine` 和 `AccountingPeriod`。模拟发票分别引用有效收货或出货行。
- **页面与接口**：采购与销售对账、发票、收付款记录、账龄及凭证。`/purchase-invoices`、`/sales-invoices`、`/payments`，后续 `/journal-entries`。
- **规则与验收**：累计开票不得超来源可开数量，累计核销不得超发票未结余额。同一来源动作只过账一次。总账启用后借贷合计必须相等；锁定期间禁止新过账，纠错使用反向记录。验收：模拟发票 100，分次核销 60 和 40 后余额为 0，尝试多核销 1 被拒绝。先用预设账户进行演示，税务和正式成本政策单独设计。[ERPNext 收付款](https://docs.frappe.io/erpnext/payment-entry) · [会计凭证](https://docs.frappe.io/erpnext/journal-entry)

## M10 报表、追溯与运行维护

- **功能**：采购进度、库存台账、工单达成、销售发货、应收应付、SN 追溯；备份和恢复演练。
- **数据**：按已过账单据和流水生成只读查询。数量、金额和时间口径写在报表说明中；库存余额与有效流水定期对账。
- **页面与接口**：报表按期间、仓库和单据状态筛选，可回查原单。`/reports/procurement`、`/reports/inventory`、`/reports/production`、`/reports/sales`、`/reports/receivables`。导出遵循同样的权限和筛选条件。
- **规则与验收**：从库存报表某个增量回查到对应收货明细，从整机 SN 追溯板卡和出货。从备份在隔离位置恢复后，抽查同一批单据与库存汇总是否一致。
