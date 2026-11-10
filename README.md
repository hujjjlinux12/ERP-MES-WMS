# 显示器制造 ERP–MES–WMS

以 C# 开发，按软件工程流程从零建设的制造业务模拟项目。当前在 `LearningErp.Api/Erp` 中实现了物料、供应商和采购草稿，并已接入 SQLite 和网页工作台。整套设计与开发顺序从 [ERP 总体设计](docs/11-erp-design-index.md) 开始阅读。

## 当前项目定位（优先于早期草案）

本项目仅用于模拟与学习，不用于真实生产。工厂规模和 SMT 是业务背景；早期文档中的上线签字、硬件部署、财务安排及 6–9 个月估算不再作为当前交付前置条件或承诺。按需求、设计、编码、测试逐步完成，先 ERP，再扩展 MES/WMS。

当前学习项目使用 .NET 10 SDK。从仓库根目录可运行：

```powershell
dotnet run --project .\LearningErp.Api\Erp\Erp.csproj
```

打开启动日志中的地址进入工作台。建议在 `LearningErp.Api/Erp` 目录中运行 `dotnet run` 和 `dotnet ef`，便于确认相对路径 `erp.db` 指向同一数据库。

以下是早期内存演示代码的运行方式，供参考：

```powershell
dotnet build src/Manufacturing.Erp.Api/Manufacturing.Erp.Api.csproj
dotnet run --project tests/Manufacturing.Erp.DomainChecks
dotnet run --project src/Manufacturing.Erp.Api --urls http://localhost:5080
```

启动后使用 [HTTP 示例](samples/purchasing.http)；健康检查 `/health`，初始化资料 `/api/catalog`。自动化领域检查是控制台程序，失败时以非零退出码结束。

## 已确认背景

- 工厂约 400 人，6 条生产线，日均产量约 500 台。
- 当前没有 ERP 和财务软件；开发团队 2 人。
- 第一期目标：打通采购、仓储、生产与出货。
- 业务产品为显示器；不假定具备 TCL 的组织结构或业务流程。
- 工厂有 SMT 产线，其管理功能由本项目新建；6 条产线是否含 SMT 线待确认。

## 文档入口

| 文档 | 用途 |
| --- | --- |
| [项目章程与交付计划](docs/01-project-charter.md) | 范围、角色、阶段门与排期方法 |
| [产品需求规格说明书](docs/02-product-requirements.md) | 业务流程、功能要求、验收标准 |
| [架构与数据设计](docs/03-architecture.md) | C# 技术方向、模块边界、数据规则 |
| [工程与质量规范](docs/04-engineering-quality.md) | 需求变更、开发、测试、发布与运维 |
| [调研与决策清单](docs/05-discovery-decisions.md) | 待确认事项与决策记录 |
| [交付模板](docs/06-delivery-templates.md) | 详细需求、接口、测试、上线模板 |
| [SMT 业务与追溯补充](docs/07-smt-requirements.md) | SMT 工单、料卷、上料防错、板卡与整机衔接 |
| [ERP 第一期详细设计](docs/08-erp-phase1.md) | 模拟边界、采购需求、页面、数据、接口与验收 |
| [模块设计与实施路线](docs/09-module-design-roadmap.md) | 当前进度、全模块边界、实施顺序及下一步采购列表设计 |
| [ERPNext 采购对照设计](docs/10-erpnext-procurement-reference.md) | ERPNext 官方文档与源码、本项目采购规则和实施步骤 |
| [ERP 总体设计](docs/11-erp-design-index.md) | 总体范围、业务链、技术架构及通用数据规则 |
| [ERP 模块详细设计](docs/12-erp-module-design.md) | 十个模块的业务、数据、页面、接口与验收 |
| [ERP 开发与验证计划](docs/13-erp-development-plan.md) | 分步开发、依赖关系、当前任务及贯通验收 |

## 文档状态

版本：0.1；日期：2026-11-06。以上为初始草案，不代表业务方已签字确认。技术选型为建议，未锁定依赖版本。正式开发前，需要工厂负责人、采购、仓库、生产和品质负责人共同评审需求。

推进顺序：现状调研 → 业务蓝图 → 详细需求与原型 → 数据/接口设计 → 增量开发与测试 → 单线试点 → 分批上线。
