# Post-Encryption

一个基于 .NET（WPF）的简单报文加密/解密桌面应用。支持在界面中输入报文与密钥，点击按钮进行加密或解密，适合演示、日常测试与轻量级使用。

## 功能特性
- 简洁界面：标题、报文输入框、密钥输入框、加密按钮、解密按钮
- 即时操作：加密结果以 Base64 输出，便于复制与传输
- 轻量实现：默认使用 AES-256-CBC（SHA256 派生密钥），可替换为其他算法

## 界面说明
- 标题：显示“报文加密/解密服务”
- 报文框：输入或显示待加密/解密的文本内容
- 密钥框：输入用于加密/解密的密钥字符串（建议长度≥8）
- 加密按钮：将报文加密为 Base64 文本（携带随机 IV）
- 解密按钮：将 Base64 文本还原为明文

## 加解密策略（默认实现）
- 算法：AES-256（CBC 模式）
- 密钥派生：`SHA256(keyText)` → 32 字节作为 AES Key
- 初始向量（IV）：随机 16 字节，前置在密文前，再整体做 Base64 输出
- 编码：`UTF-8` 文本；密文以 Base64 文本展示
- 异常处理：解密失败（密钥错误或格式错误）会提示错误

> 注意：此策略用于演示与一般用途，非高安全场景。若用于生产，请引入标准密钥管理、版本化参数、认证加密（如 AES-GCM）与更完善的异常与日志策略。

## 环境要求
- Windows 10/11
- .NET SDK（建议 8.0 或兼容版本）

## 快速开始
1. 安装 .NET SDK（https://dotnet.microsoft.com/download）
2. 在项目根目录执行：
   - `dotnet build`
   - `dotnet run`（首次运行会生成并启动 WPF 窗口）

## 目录结构（初始化后）
```
Post-Encryption/
├─ README.md
├─ .gitignore
├─ Post-Encryption.sln
└─ src/
   └─ PostEncryptionApp/
      ├─ App.xaml
      ├─ App.xaml.cs
      ├─ MainWindow.xaml
      ├─ MainWindow.xaml.cs
      └─ PostEncryptionApp.csproj
```

## 后续计划
- 增加算法选择（AES-GCM、ChaCha20-Poly1305、XOR 演示等）
- 增加密钥强度校验与提示
- 增加输入/输出区域复制与清理按钮
- 增加异常提示的用户体验优化