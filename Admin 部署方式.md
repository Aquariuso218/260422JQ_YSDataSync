# **Admin 部署方式**

这次改成 `IIS` 是对的。对这台 `Windows Server 2019` 来说，`IIS` 比 Docker 更省事，尤其你们现在是前后端分离但都在同一台服务器上，最简单高效的做法是：

同一个 IIS 站点  
根路径 `/` 部署前端 `dist`  
子应用 `/api` 部署后端 `WebApi`

最终访问效果：

1. 前端首页：`http://服务器IP/`
2. 后端接口：`http://服务器IP/api/...`
3. SignalR：`http://服务器IP/api/msgHub`
4. Swagger：`http://服务器IP/api/swagger/index.html`

这个结构最适合你当前项目，因为你前端现在本来就是单独打包，后端也是标准 `.NET 8 WebApi`。你当前相关文件我已经对齐过了：
[.env.production](D:/1a_Projects/A_Project/金桥/260416数据同步/YYCAdmin/YYCAdmin-Vue3/.env.production)  
[appsettings.json](D:/1a_Projects/A_Project/金桥/260416数据同步/YYCAdmin/YYCAdmin-Net8/ZR.Admin.WebApi/appsettings.json)  
[Program.cs](D:/1a_Projects/A_Project/金桥/260416数据同步/YYCAdmin/YYCAdmin-Net8/ZR.Admin.WebApi/Program.cs)

**整体结构**
建议服务器目录这样放：

```text
D:\Deploy\YYCAdmin
├─ Web
│  ├─ index.html
│  ├─ assets
│  └─ web.config
└─ Api
   ├─ ZR.Admin.WebApi.dll
   ├─ web.config
   ├─ appsettings.Production.json
   ├─ wwwroot
   ├─ logs
   └─ DataProtection
```

**一、先改生产配置**
先把前端生产环境地址改掉，不要再用 `localhost`。

把 [`.env.production`](D:/1a_Projects/A_Project/金桥/260416数据同步/YYCAdmin/YYCAdmin-Vue3/.env.production) 改成：

```env
ENV = 'production'
VITE_APP_BASE_API = '/api'
VITE_APP_ROUTER_PREFIX = '/'
VITE_APP_UPLOAD_URL = '/Common/UploadFile'
VITE_APP_SOCKET_API = '/api/msgHub'
VITE_BUILD_COMPRESS = gzip
```

说明：

1. `VITE_APP_BASE_API` 改成 `/api`
2. `VITE_APP_SOCKET_API` 改成 `/api/msgHub`
3. 这样前端和后端同域，不需要再处理跨域代理

后端不要直接拿当前 [appsettings.json](D:/1a_Projects/A_Project/金桥/260416数据同步/YYCAdmin/YYCAdmin-Net8/ZR.Admin.WebApi/appsettings.json) 去上线，因为这个文件里现在有 `//` 注释，标准 JSON 会报错。生产环境请单独准备 `appsettings.Production.json`。

建议内容至少调整这些项：

```json
{
  "dbConfigs": [
    {
      "Conn": "Data Source=实际数据库IP;Initial Catalog=ZRAdmin;User ID=sa;Password=实际密码;Encrypt=True;TrustServerCertificate=True;",
      "DbType": 1,
      "ConfigId": "0",
      "IsAutoCloseConnection": true
    },
    {
      "Conn": "Data Source=10.10.200.40,62433;Initial Catalog=UFDATA_005_2019;User ID=sa;Password=实际密码;Encrypt=True;TrustServerCertificate=True;",
      "DbType": 1,
      "ConfigId": "1",
      "IsAutoCloseConnection": true
    }
  ],
  "corsUrls": [
    "http://服务器IP"
  ],
  "Upload": {
    "uploadUrl": "http://服务器IP/api",
    "localSavePath": "",
    "maxSize": 15,
    "requestLimitSize": 50
  },
  "ysConfig": {
    "YtenantId": "你的租户",
    "AuthBaseUrl": "https://c2.yonyoucloud.com/iuap-api-auth/open-auth/selfAppAuth/base/v1/getAccessToken",
    "GatewayBaseUrl": "https://c2.yonyoucloud.com/iuap-api-gateway",
    "appKey": "你的appKey",
    "appSecret": "你的appSecret"
  },
  "U8Config": {
    "baseUrl": "http://U8服务地址"
  }
}
```

注意：

1. 所有 `localhost` 都要换成真实地址
2. 不能写注释
3. `urls` 在 IIS 下不是重点，IIS 会接管监听端口

**二、安装服务器必要环境**
在 `Windows Server 2019` 上先装 IIS。

建议在“服务器管理器 -> 添加角色和功能”里安装这些：

1. `Web Server (IIS)`
2. `WebSocket Protocol`
3. `Static Content`
4. `Default Document`
5. `HTTP Errors`
6. `Request Filtering`
7. `Management Console`

如果你喜欢命令行，也可以用 PowerShell：

```powershell
Install-WindowsFeature Web-Server,Web-Static-Content,Web-Default-Doc,Web-Http-Errors,Web-Filtering,Web-WebSockets,Web-Mgmt-Console
```

然后安装 `.NET 8 Hosting Bundle`。  
官方文档说明：IIS 托管 ASP.NET Core 需要安装 Hosting Bundle，它会安装 `.NET Runtime` 和 `ASP.NET Core Module`。如果先装了 Hosting Bundle、后装 IIS，要再运行一次 Hosting Bundle 做修复。  
参考：
[Host ASP.NET Core on Windows with IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-9.0)

下载入口：
[Install the .NET Hosting Bundle](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-9.0#install-the-net-hosting-bundle)

前端如果使用 Vue history 路由，还要安装 `IIS URL Rewrite`，不然刷新页面会 404。  
官方说明：
[Using the URL Rewrite Module](https://learn.microsoft.com/en-us/iis/extensions/url-rewrite-module/using-the-url-rewrite-module)  
下载：
[URL Rewrite 2.1](https://www.iis.net/downloads/microsoft/url-rewrite)

**三、发布前端**
在前端目录执行：

```bash
yarn install
yarn build:prod
```

生成目录一般是：

```text
D:\1a_Projects\A_Project\金桥\260416数据同步\YYCAdmin\YYCAdmin-Vue3\dist
```

把 `dist` 里的全部文件复制到：

```text
D:\Deploy\YYCAdmin\Web
```

然后在 `D:\Deploy\YYCAdmin\Web` 新建一个 `web.config`，内容如下：

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="VueRouter" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
            <add input="{REQUEST_URI}" pattern="^/api($|/)" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
    <staticContent>
      <remove fileExtension=".json" />
      <mimeMap fileExtension=".json" mimeType="application/json" />
    </staticContent>
  </system.webServer>
</configuration>
```

这个文件的作用：

1. 支持 Vue history 路由刷新不报 404
2. 避开 `/api`，不影响后端接口

**四、发布后端**
在后端项目目录执行发布：

```bash
dotnet publish D:\1a_Projects\A_Project\金桥\260416数据同步\YYCAdmin\YYCAdmin-Net8\ZR.Admin.WebApi\ZR.Admin.WebApi.csproj -c Release -o D:\Publish\YYCAdmin\Api
```

把 `D:\Publish\YYCAdmin\Api` 下的全部内容复制到：

```text
D:\Deploy\YYCAdmin\Api
```

然后把你准备好的 `appsettings.Production.json` 放进去。

说明：

1. `dotnet publish` 后通常会自动生成 IIS 用的 `web.config`
2. 后端这个 `web.config` 一般不用手改，优先用发布产物自带的版本

**五、IIS 站点配置**
推荐这样建：

1. 新建应用程序池 `YYCAdmin_Web`
2. 新建应用程序池 `YYCAdmin_Api`

这两个应用程序池都设置为：

1. `.NET CLR Version`：`No Managed Code`
2. `Managed pipeline mode`：`Integrated`
3. `Enable 32-Bit Applications`：`False`

然后新建站点：

1. 站点名：`YYCAdmin`
2. 物理路径：`D:\Deploy\YYCAdmin\Web`
3. 绑定：`http`，端口 `80`
4. 主机名：可空，或者填域名
5. 应用程序池：`YYCAdmin_Web`

站点建好后，在这个站点下面添加一个“应用程序”：

1. 别名：`api`
2. 物理路径：`D:\Deploy\YYCAdmin\Api`
3. 应用程序池：`YYCAdmin_Api`

这样外部访问就是：

1. `/` 走前端
2. `/api` 走后端

**六、目录权限**
你的后端在 [Program.cs](D:/1a_Projects/A_Project/金桥/260416数据同步/YYCAdmin/YYCAdmin-Net8/ZR.Admin.WebApi/Program.cs) 里会创建 `DataProtection` 目录，还会用到 `wwwroot`、日志、上传目录，所以必须给后端应用池身份写权限。

给 `IIS AppPool\YYCAdmin_Api` 赋权：

1. `D:\Deploy\YYCAdmin\Api\wwwroot`：`修改`
2. `D:\Deploy\YYCAdmin\Api\logs`：`修改`
3. `D:\Deploy\YYCAdmin\Api\DataProtection`：`修改`

如果你图省事，也可以直接给：

```text
D:\Deploy\YYCAdmin\Api
```

赋 `修改` 权限，但生产上建议只给必要目录。

前端目录 `D:\Deploy\YYCAdmin\Web` 一般只需要读取权限。

**七、防火墙**
放行端口 `80`，如果后续上 HTTPS 再放行 `443`。

PowerShell：

```powershell
New-NetFirewallRule -DisplayName "IIS HTTP 80" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
New-NetFirewallRule -DisplayName "IIS HTTPS 443" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

**八、上线验证**
按这个顺序验收最省时间：

1. 访问 `http://服务器IP/`，前端首页能打开
2. 访问 `http://服务器IP/api/swagger/index.html`，Swagger 能打开
3. 登录功能正常
4. 文件上传正常
5. SignalR 正常
6. 调用 U8、YS、数据库都正常

**九、更新流程**
以后更新就很简单：

1. 前端重新执行 `yarn build:prod`
2. 后端重新执行 `dotnet publish -c Release`
3. 覆盖服务器目录
4. 在 IIS 里回收 `YYCAdmin_Api` 应用池，必要时也回收 `YYCAdmin_Web`

建议更新时先停应用池，再覆盖文件，最后启动应用池，避免 DLL 被占用。

**十、常见问题**
1. `500.31 ANCM Failed to Find Native Dependencies`
原因通常是没装 `.NET 8 Hosting Bundle`，或者装完没重启 IIS。  
处理：重装 Hosting Bundle，然后执行：
```powershell
iisreset
```

2. 前端刷新路由 404  
原因通常是没装 `URL Rewrite`，或者前端 `web.config` 没配好。

3. SignalR 连接不上  
重点检查：
`WebSocket Protocol` 是否启用  
`VITE_APP_SOCKET_API` 是否是 `/api/msgHub`

4. 后端启动直接报配置错误  
你这个项目要特别注意：生产配置不能带 `//` 注释。  
当前 [appsettings.json](D:/1a_Projects/A_Project/金桥/260416数据同步/YYCAdmin/YYCAdmin-Net8/ZR.Admin.WebApi/appsettings.json) 里就有这种风险，生产一定用单独的 `appsettings.Production.json`。

5. 上传失败或登录后部分功能报权限错误  
通常是 `wwwroot`、`logs`、`DataProtection` 没写权限。

**最简结论**
对你这个项目，最简单高效的 IIS 方案就是：

1. 前端打包到 `D:\Deploy\YYCAdmin\Web`
2. 后端发布到 `D:\Deploy\YYCAdmin\Api`
3. IIS 一个站点挂前端根目录
4. IIS 一个子应用 `/api` 挂后端
5. 前端 API 地址改成 `/api`
6. 安装 `.NET 8 Hosting Bundle`、`WebSocket`、`URL Rewrite`

如果你愿意，我下一步可以直接继续帮你整理三份可直接落地的文件内容：

1. 前端最终版 [`.env.production`](D:/1a_Projects/A_Project/金桥/260416数据同步/YYCAdmin/YYCAdmin-Vue3/.env.production)
2. 后端 `appsettings.Production.json` 模板
3. 前端 `web.config` 完整成品