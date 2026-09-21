# wpf-harness

一个基于 **WPF (.NET 10)** 的可扩展 AI Agent 客户端：内置类 [pi](https://github.com/badlogic/pi-mono) 风格的极简 Agent Harness 内核，支持多 LLM 接入（OpenAI 兼容协议）、可插拔运行模式（对话 / ReAct）与工具扩展。

> 由旧项目 chatgpt-wpf-ui 重构而来，旧架构（LiteDB + 手写 OpenAI 客户端）已废弃。

## 目录结构

遵循 .NET 通用约定：**仓库根 slnx + `src/` 放产品代码 + `tests/` 放测试**。

```
WpfHarness.slnx             # 解决方案（根目录，通过 slnx 引用下列项目）
├─ src/
│  ├─ WpfHarness/           # WPF 界面（MVVM, WPF-UI + MdXaml）
│  │  ├─ ViewModels/        # MainViewModel / ChatSessionViewModel / SettingsViewModel
│  │  └─ Views/             # HomeView（首页输入卡）/ ChatView（会话）/ SettingsView
│  └─ WpfHarness.Harness/   # Agent Harness 内核（无 UI 依赖，可独立测试）
│     ├─ Abstractions/      # IChatProvider / IAgentMode / ToolDef / IHarnessExtension / AgentEvent
│     ├─ Providers/         # OpenAI 兼容 SSE 流式 + Mock（离线）
│     ├─ Modes/             # ChatMode / ReActMode（原生工具调用）/ ReActTextMode（文本协议）
│     ├─ Tools/             # ToolRegistry + 内置工具（get_current_time / http_get）
│     ├─ Extensions/        # 扩展示例（内置工具以扩展注册）
│     ├─ Storage/           # JSON 会话存储 + 机器绑定加密（ApiKey）
│     └─ HarnessBuilder/Engine # 组合根 + 事件流驱动引擎
└─ tests/
   └─ WpfHarness.Harness.Tests/  # xUnit：Session 标题派生 / 工具注册 / ApiKey 加解密
```

> 说明：WPF 只是项目类型，目录组织遵循的是 .NET 生态的通用约定（MahApps.Metro、
> MaterialDesignInXamlToolkit、eShopOnWeb 等均为根 sln + `src/<项目>`），
> 把 sln 放在 `src/` 内属少数派做法。

核心概念（pi 风格）：

- **Provider**：任意 LLM 服务实现 `IChatProvider`，输出统一的流式事件（文本增量 / 工具调用 / 结束）；
- **Mode**：`IAgentMode` 实现完整推理循环（ReAct = Thought → Action → Observation 循环），通过 `AgentEvent` 广播过程；
- **Tool**：`ToolDef` = JSON Schema 描述 + 执行委托，注册进 `IToolRegistry`；
- **Extension**：通过 `IHarnessExtension.Configure(HarnessBuilder)` 注入工具/模式/Provider/事件订阅器。

## 快速开始

```bash
# 运行客户端（仓库根执行）
dotnet run --project src/WpfHarness/WpfHarness.csproj

# 运行 Harness 内核单测
dotnet test tests/WpfHarness.Harness.Tests/WpfHarness.Harness.Tests.csproj

# 构建整个解决方案
dotnet build WpfHarness.slnx
```

首次启动内置 **Mock Provider**（离线），可直接体验对话与 ReAct 工具调用。接入真实模型：侧栏底部 ⚙ 设置 → 添加模型服务：

| 字段 | 示例 |
|---|---|
| 类型 | `openai-compat` |
| BaseUrl | `https://api.openai.com/v1`（或 GLM/DeepSeek/Ollama 等兼容端点） |
| 模型 | `gpt-4o-mini` / `glm-4.6` / `deepseek-chat` |
| API Key | 明文输入，落盘机器绑定加密存储 |

数据目录：`%APPDATA%\wpf-harness\`（config.json + sessions/*.json）。

## Roadmap

- [x] MVP：多 Provider / 对话 + ReAct 双模式 / 工具扩展 / 会话持久化
- [ ] MCP 工具接入、更多模式（Plan-and-Execute、ReWOO）
- [ ] 任务分组、搜索（Ctrl+K）、附件输入
- [ ] 多模态消息（图片）、导出 Markdown

## License

MIT
