# ERP 第一期：需求与详细设计

版本 0.1，2026-11-06；状态：模拟设计基线。本文及 README 的模拟定位优先于早期真实工厂上线假设。工程规范保留需求追踪、设计、代码审查和测试；真实生产签字、机房和财税手续不阻塞模拟开发。

## 1. 产品目标与交付顺序

模拟一家显示器工厂的业务过程，最终贯通采购、库存、BOM、生产和销售。第一阶段先建设 ERP；SMT/MES 和高级 WMS 后续实现。

| 增量 | 范围 | 当前状态 |
| --- | --- | --- |
| E1 | 示例基础资料、采购提交审批、分批收货、库存流水 | 代码骨架已编写，未运行验证 |
| E2 | 数据库持久化、迁移、事务、并发及幂等约束 | 待开发 |
| E3 | 基础资料维护界面、采购及库存界面、用户权限 | 待设计/开发 |
| E4 | 多层 BOM、板卡/整机工单、领退料及完工 | 待详细设计 |
| E5 | 销售订单、发货、退货及采购异常流程 | 待详细设计 |

财务总账、税务、设备联锁、自动 MRP 不在首个增量。当前没有为未开发模块提供空壳完成状态。

## 2. 模拟约定

使用单工厂、单币种 CNY、物料基本单位 PCS；不计税、不换算单位、不做质检放行和财务凭证。金额为数量乘以单价，订单合计保留两位小数，采用 AwayFromZero 舍入。这些是模拟假设，不能当作真实工厂规则。

收货即进入仓库可用库存。未来接入品质后，需要扩展待检库存，不能把当前简化流程直接沿用。数量最多三位小数，单价最多四位小数，所有计算使用 decimal。

角色设计：采购员维护及提交订单；审批员审批；仓管员收货；查询者只读。首个内存 API 尚未实现认证，不把 actor 文本视为可信身份。权限验收安排在 E3。

## 3. 需求与状态

| 编号 | 业务要求 | 验收标准 |
| --- | --- | --- |
| ERP-PUR-001 | 创建采购单 | 供应商及物料存在；至少一行；数量大于 0、单价不小于 0；不允许重复料号行 |
| ERP-PUR-002 | 提交和审批 | Draft → Submitted → Approved；不允许草稿直接审批或未审批收货 |
| ERP-PUR-003 | 分批收货 | Approved → PartiallyReceived → Received；全部行收满才为 Received |
| ERP-PUR-004 | 防止超收 | 每行累计收货不得超过订单数量；任一行失败时整次请求无库存或收货副作用 |
| ERP-PUR-005 | 重复请求处理 | 收货 requestId 全局唯一；相同载荷返回原结果，不重复入库；相同键不同载荷返回冲突 |
| ERP-INV-001 | 库存与流水 | 余额按物料和仓库累计；每笔流水关联收货及订单行 |
| ERP-PUR-006 | 留存操作者 | 提交、审批、收货保留时间和操作者；认证接入前 actor 仅模拟输入 |

本增量不含改单、撤回、驳回、关闭、退货或冲销；因此记录不可编辑或删除。后续增加时需要补齐状态与流水反向操作，不通过改余额实现。

## 4. 页面原型说明（待实现）

- 基础资料页：物料编码/名称/单位、供应商、仓库，首版只展示初始化数据。
- 采购列表：订单号、供应商、状态、金额，支持状态筛选；点入详情。
- 采购详情：订单行、累计已收、剩余数量；根据状态显示提交、审批、收货操作。
- 收货对话框：仓库、行号、本次数量，默认不自动填写全部剩余量；提交后展示收货编号。
- 库存页：物料、仓库、现存量；流水页展示时间、来源、数量和操作者。

页面必须显示模拟环境标识；后端仍执行校验，不能依赖按钮隐藏保障状态正确性。

## 5. 数据设计

| 实体 | 核心字段与约束 |
| --- | --- |
| Material | Code 唯一、Name、Unit |
| Supplier / Warehouse | Code 唯一、Name |
| PurchaseOrder | Id、Number 唯一、SupplierCode、Status、CreatedAt、提交与审批人/时间 |
| PurchaseLine | Id、OrderId、MaterialCode、Quantity、UnitPrice、ReceivedQuantity；订单内料号唯一 |
| Receipt | Id、RequestId 唯一、OrderId、WarehouseCode、Actor、ReceivedAt、Lines |
| ReceiptLine | ReceiptId、PurchaseLineId、Quantity |
| StockBalance | MaterialCode + WarehouseCode 唯一、Quantity、并发版本（E2） |
| StockLedger | Id、ReceiptId、OrderId、OrderLineId、MaterialCode、WarehouseCode、Quantity、Time、Actor |

E1 使用单进程内存和全局锁串行化写入，用于展示规则；不代表多实例并发方案。E2 计划采用 SQLite + EF Core 作为本地模拟数据库，事务内写收货、流水、余额和订单累计量，通过唯一约束和并发版本保障一致性；实施时验证依赖版本。未来更换生产级数据库时重新验证并发语义。

## 6. 接口契约

| 方法和路径 | 输入 | 返回 |
| --- | --- | --- |
| GET /health | 无 | 运行状态与存储类型 |
| GET /api/catalog | 无 | 示例物料、供应商、仓库 |
| GET /api/purchase-orders | 无 | 订单列表 |
| GET /api/purchase-orders/{id} | 订单 ID | 订单详情，未知 ID 返回 404 |
| POST /api/purchase-orders | supplierCode、lines(materialCode, quantity, unitPrice) | 201，订单详情及 Location |
| POST /api/purchase-orders/{id}/submit | actor | 更新订单 |
| POST /api/purchase-orders/{id}/approve | actor | 更新订单 |
| POST /api/purchase-orders/{id}/receipts | requestId、warehouseCode、actor、lines(orderLineId, quantity) | 收货记录；幂等重试返回原记录 |
| GET /api/inventory/balances | 无 | 库存余额 |
| GET /api/inventory/ledger | 无 | 库存流水 |

业务验证错误返回 400，状态/幂等冲突返回 409，不存在资源返回 404；格式绑定错误由 ASP.NET Core 处理。日期使用 UTC，状态以字符串序列化。列表分页、统一身份和持久化审计在后续实现。

## 7. 测试追踪与完成情况

领域检查覆盖 ERP-PUR-002 至 005 和 ERP-INV-001：非法状态收货、部分收货、重复请求、异载荷复用键、超收、失败不产生副作用、最终收满状态和流水余额一致性。E1 暂不声称完成权限、数据库恢复、HTTP 集成或页面测试。

当前验证：本机未找到 .NET SDK，未执行构建及领域检查；代码和测试程序已准备。下一步安装或提供 .NET 10 SDK 后先运行检查，修复失败，再进入 E2 持久化；不得把未运行的检查标记为通过。
