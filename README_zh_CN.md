# Sentinel-CSharp

Sentinel-CSharp是Sentinel的C#实现，提供了Sentinel的核心功能，包括单文件和文件夹AppBinary扫描、上报、生成OpenResty配置等功能。

## 功能

- 多插件支持：内置三种插件类型
  - SingleFile：处理单个文件
  - SubFolder：根据策略处理文件夹中文件
  - PythonPluginLoader：加载并运行Python插件
    - 提供Python版SingleFile插件示例
- 后台服务：支持作为守护进程运行，定期扫描文件变更
- 数据上报：与Librarian服务集成，支持自动上报文件信息
- 可扩展性：通过插件系统支持自定义文件处理逻辑
- OpenResty集成：提供OpenResty配置示例，支持文件下载验证和访问控制

## 使用

### 命令行参数

Sentinel-CSharp支持多种运行模式：

- 守护进程模式：读取配置文件并持续运行

```
# 守护进程模式（读取配置文件并持续运行）
dotnet Sentinel.dll daemon [options]
# 或简写
dotnet Sentinel.dll d [options]

# 选项：
# -n, --no-report     只记录到控制台，不向服务器上报
```

- 单插件模式：测试用

```
# 单文件插件
dotnet Sentinel.dll singlefile [options]
# 或简写
dotnet Sentinel.dll sf [options]

# 子文件夹插件
dotnet Sentinel.dll subfolder [options]
# 或简写
dotnet Sentinel.dll subf [options]

# Python插件
dotnet Sentinel.dll pythonpluginloader [options]
# 或简写
dotnet Sentinel.dll ppl [options]
# 选项
# -s, --script-path <PATH>  Python脚本路径
# -c, --script-class <NAME> Python类名

# 通用选项：
# -d, --dir <DIR>           指定要扫描的目录
# -n, --dry-run             干运行模式，不计算SHA256校验和
# --chunk-size <SIZE>       分块大小（字节），默认为67108864（64 MiB）
# --debug                   开启调试日志
# --plugin-base-dir <DIR>   指定插件路径，默认为"./plugins"
```

### 配置文件

配置文件`appsettings.json`包含以下主要设置：

```json
{
  "SystemConfig": {
    "LibrarianUrl": "http://librarian",                 // Librarian服务地址
    "LibrarianRefreshToken": "",                        // 认证令牌
    "DbPath": "./sentinel.db",                          // 数据库路径
    "PluginBaseDir": "./plugins",                       // 插件目录
    "LibraryConfigs": [                                 // 库配置列表
      {
        "PluginName": "SingleFile",                     // 插件名称
        "DownloadBasePath": "/library",                 // 下载路径前缀
        "PluginConfig": {                               // 插件配置
          "LibraryName": "Library1",                    // 库名称
          "LibraryFolder": "/path/to/library1",         // 库文件夹路径
          "ChunkSizeBytes": "67108864",                 // 分块大小 (64MB)
          "ForceCalcDigest": "false"                    // 是否强制计算摘要
        }
      }
    ],
    "LibraryScanIntervalMinutes": 1440                  // 扫描间隔（分钟）
  },
  "SentinelConfig": {
    "Hostnames": ["https://dl1.example.com:1234"],      // URL列表
    "NeedToken": "true",                                // 是否需要令牌
    "GetTokenUrlPath": "/get_token",                    // 获取令牌路径
    "DownloadFileUrlPath": "/download_file"             // 下载文件路径
  }
}
```

其中`PluginConfig`部分，根据插件类型不同，配置项也不同
- 共有插件配置
  - `LibraryName`：库名称
  - `LibraryFolder`：库文件夹路径
  - `ChunkSizeBytes`：分块大小（字节）
  - `ForceCalcDigest`：扫描时是否强制计算摘要
- `SingleFile`插件配置
  - 仅有共有插件配置
- `SubFolder`插件配置
  - 共有插件配置
  - `MinDepth`：最小深度，最小为1
  - `ScanPolicy`：扫描策略
    - `UntilAnyFile`：直到存在文件的文件夹为一个Binary（默认）
    - `UntilNoFolder`：直到只有文件的文件夹为一个Binary
- `PythonPluginLoader`插件配置
  - 共有插件配置
  - `PythonScriptPath`：Python脚本路径
  - `PythonClassName`：Python类名
  - `PythonScriptCustomConfig`：自定义配置，为一个字典

### OpenResty集成

提供了OpenResty配置示例，可用于设置文件下载验证和访问控制：

- 配置文件：`config.json`包含密钥和目录映射，由Sentinel生成
- Lua脚本：`sentinel_config.lua`加载配置（硬编码`config.json`位置）
- Nginx配置：包含初始化脚本、限制规则和位置配置

#### 典型下载流程

1. 客户端请求下载令牌 (`/get_token`)
2. OpenResty验证请求并生成JWT令牌
3. 客户端使用令牌请求文件 (`/download_file`)
4. OpenResty验证令牌、解密路径并提供文件下载
5. 应用速率限制和连接限制防止滥用

## 插件开发

Sentinel-CSharp支持编写自定义插件：

- C#插件：实现IPlugin接口
- Python插件：继承PluginBase，通过PythonPluginLoader加载
