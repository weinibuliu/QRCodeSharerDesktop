# QRCodeSharer-Desktop

此项目是 [QRCodeSharer](https://github.com/weinibuliu/QRCodeSharer) 的桌面实现。

相较于移动端，桌面端仅保留了同步与设置功能。

## 系统要求
- Windows 10 1809 (推荐) / Windows 10 1607 (最低)
- .NET 10.0 Runtime

>[!NOTE]
> 项目没有自包含 .NET 10.0 Runtime，用户需要自行[安装](https://builds.dotnet.microsoft.com/dotnet/Runtime/10.0.1/dotnet-runtime-10.0.1-win-x64.exe)。

## 构建
项目使用 .NET10 + WPF 开发。
```bash
dotnet build
```