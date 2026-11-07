# Unity MCP

已嵌入 CoderGamester MCP Unity 1.5.0，适用于本项目的 Unity 2022.3。源码在 Packages/com.gamelovers.mcp-unity。

Node 服务已安装依赖并构建；Codex 项目配置在 .codex/config.toml。配置使用本机 localhost:8090 和当前项目的设置、认证文件路径。认证内容由 Unity 生成在 Library/McpUnity/bridge-token，不写进项目配置。

Unity 完成编译后，通过 Tools → MCP Unity → Server Window 查看或启动服务。Codex 重新加载 MCP 配置后可使用场景、对象、控制台、播放模式等工具。

验证记录：Node 的 132 项测试通过，MCP 握手及 34 项工具列表读取通过。已通过 MCP 实际读取当前 03_Performance 场景，Unity 端连接确认成功。ReTuneMcpBootstrap 在导入完成后初始化服务，自动启动仍由项目设置的 AutoStartServer 控制。

换电脑或重新检出后，在 Server~ 目录执行 npm ci 和 npm run build，并更新 .codex/config.toml 中的本机路径。
