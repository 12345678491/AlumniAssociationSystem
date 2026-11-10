# 基于.NET的校友会系统（ASP.NET Core Web）

依据《基于.NET的校友会系统_业务流程与模块划分.docx》实现的完整可运行系统。

## 部署到公网（GitHub + Render 免费方案）

1. 把本仓库推送到 GitHub；
2. 登录 [Render](https://render.com)（支持 .NET/Docker，有免费实例）；
3. New → Blueprint → 绑定本 GitHub 仓库，Render 会读取 `render.yaml` 自动构建 Docker 镜像并启动；
4. 部署完成后 Render 会分配公网地址（如 `https://alumni-association-system.onrender.com`），任何人可访问。

> 注意：Render 免费实例使用临时磁盘，重新部署后 SQLite 数据会重置（演示场景无影响）；15 分钟无访问会休眠，首次访问需等待约 30~60 秒冷启动。

## 技术栈

- **开发环境**：Visual Studio 2026（或 VS 2022 17.12+ / VS Code + dotnet CLI）
- **框架**：ASP.NET Core MVC，目标框架 `net10.0`（.NET 10）
- **数据库**：SQLite（文件 `AlumniAssociationSystem.Web/alumni.db`，首次启动自动建库并写入种子数据）
- **认证**：Cookie 认证 + 基于角色的授权（RBAC：管理员 / 校友）
- **ORM**：Entity Framework Core 10

## 打开与运行

1. 双击解决方案文件 `AlumniAssociationSystem.sln`，用 Visual Studio 2026 打开；
2. 将 `AlumniAssociationSystem.Web` 设为启动项目，按 `F5` 运行（或按 Ctrl+F5）；
3. 命令行方式：`cd AlumniAssociationSystem.Web && dotnet run`，浏览器访问提示的地址。

> 首次运行会自动创建 `alumni.db` 并写入演示数据。

## 演示账号

| 角色 | 账号（手机号） | 密码 |
|------|----------------|------|
| 管理员 | 13800000000 | Admin@123 |
| 校友 | 13900000001 ~ 13900000005 | Alumni@123 |

另有待审核账号 `13700000001 / Alumni@123`（张小明），可用于演示用户审核流程。

## 模块与功能对照（对应设计文档 8 大模块）

| 模块 | 已实现功能 |
|------|-----------|
| 2.1 用户认证与权限管理 | 手机号/邮箱注册登录、密码找回入口、角色授权（管理员/校友）、注册验证码（演示模式页面显示，部署时可替换为短信/邮件）、账号审核 |
| 2.2 校友信息管理 | 校友档案维护（我的资料）、学号/院系/专业/毕业年份等核心信息、管理端用户列表 |
| 2.3 活动管理 | 活动创建/发布/结束/删除、在线报名（人数上限/截止时间控制）、取消报名、签到、报名名单导出（管理端查看） |
| 2.4 捐赠管理 | 捐赠项目创建/上下架、在线捐赠（金额预设+自定义、匿名、留言）、捐赠进度公示、捐赠明细公示、我的捐赠记录（支付为模拟网关回调） |
| 2.5 信息资讯 | 通知公告/校友动态分类展示、置顶、投稿审核、点赞、浏览量统计；招聘信息发布/审核/展示、在线投递、投递进度、我发布的职位 |
| 2.6 通讯通知 | 站内信消息中心（已读未读、删除）、业务事件自动站内信（报名成功/捐赠成功/审核通过等）、管理员群发站内信（按角色） |
| 2.7 数据统计分析 | 管理后台数据看板：用户/活动/捐赠/招聘各维度统计卡片、近30天新增用户趋势图、待办快捷入口 |
| 2.8 系统管理 | 操作日志与审计（用户审核等关键操作落库）、日志检索查看 |

## 预留扩展点

- 支付网关：`DonationsController.Donate` 中标记 `IsPaid=true` 处，替换为微信支付/支付宝回调处理；
- 短信/邮件通知：`MessagesController` 与 `DbInitializer` 中站内信发送处，可挂接 SMTP / 短信网关；
- 校友关系图谱、批量导入导出、评论/分享、二维码签到：模型与页面已预留字段与入口。

## 项目结构

```
AlumniAssociationSystem.sln
└─ AlumniAssociationSystem.Web
   ├─ Controllers/   Account / Home / Events / Donations / News / Jobs / Messages / Admin
   ├─ Models/        User / Event / Donation / News / Job / Message（含枚举）
   ├─ Data/          AppDbContext（EF Core）、DbInitializer（建库+种子数据）
   ├─ Extensions/    ClaimsPrincipal 扩展（当前登录用户）
   └─ Views/         Razor 视图（Bootstrap 5 布局，中文界面）
```
