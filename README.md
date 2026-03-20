# Nikon CheckIn - Selenium自动化项目

## 项目概述
使用Selenium和本地Edge驱动实现浏览器自动化，自动打开尼康官网登录页面。

## 项目要求
- **JDK**: Java 25+
- **Maven**: 3.6+
- **浏览器驱动**: 本地Edge驱动 (edgedriver_win64/msedgedriver.exe)

## 项目结构
```
My_Nikon_CheckIn/
├── pom.xml                           # Maven配置
├── edgedriver_win64/                 # 本地Edge驱动目录
│   └── msedgedriver.exe              # Edge driver可执行文件
├── src/
│   ├── main/
│   │   ├── java/
│   │   │   └── com/nikon/
│   │   │       └── OpenAndLogIn.java     # 主程序
│   │   └── resources/
│   │       └── logback.xml                # 日志配置
│   └── test/
└── README.md
```

## 运行方式

### 方式1: 使用Maven插件运行
```bash
mvn clean compile exec:java
```

### 方式2: 打包并运行JAR
```bash
mvn clean package
java -jar target/nikon-checkin-1.0.0.jar
```

### 方式3: 在IDE中运行
直接运行 `com.nikon.OpenAndLogIn` 类

### 控制台中文日志
项目启动时会先检测当前控制台实际使用的字符集，并把该字符集传给 Logback 的控制台输出编码，避免中文日志出现乱码。
如果在不同终端中运行（PowerShell、cmd、IDE 终端），日志会跟随当前终端字符集输出，不需要再手动修改源码里的日志编码。

## 配置说明

### 本地驱动配置
程序会自动查找项目目录下的 `edgedriver_win64/msedgedriver.exe` 文件。

### 目标URL
默认打开: `https://my.nikon.com.cn/user/level/task`

可在 `OpenAndLogIn.java` 中修改 `TARGET_URL` 常量来更改目标页面。

## 依赖项

| 依赖 | 版本 | 说明 |
|------|------|------|
| Selenium | 4.17.0 | 浏览器自动化框架 |
| SLF4J | 2.0.7 | 日志API |
| Logback | 1.4.8 | 日志实现 |

## 注意事项

1. **首次运行**: 首次启动 Selenium Edge 会自动启动并加载所有必要的组件
2. **浏览器窗口**: 程序会最大化窗口并保持可见
3. **页面加载**: 程序通过 `document.readyState` 确保页面完全渲染
4. **交互**: 按 Enter 键关闭浏览器

## 常见问题

### 驱动文件未找到
确保 `edgedriver_win64/msedgedriver.exe` 文件存在于项目根目录

### 权限问题
在Windows上运行时，确保有足够的权限执行驱动程序

### JDK版本错误
确保已安装Java 25或更高版本：
```bash
java -version
```

## 扩展功能

可基于此项目进行扩展，例如：
- 自动登录功能
- 表单填充和提交
- 数据爬取
- 截图和录像
- 性能测试

## 许可证
MIT
