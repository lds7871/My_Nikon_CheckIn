@echo off
chcp 65001 > nul
setlocal enabledelayedexpansion

echo ========================================
echo     Nikon CheckIn 自动化程序
echo ========================================
echo.

REM 获取当前目录
set CURRENT_DIR=%~dp0
cd /d %CURRENT_DIR%

REM 设置JRE路径
set JAVA_EXE=%CURRENT_DIR%jre-temp\bin\java.exe

REM 检查Java是否存在
if not exist "%JAVA_EXE%" (
    echo 错误：未找到 JRE 环境
    echo 期望路径: %JAVA_EXE%
    echo.
    pause
    exit /b 1
)

echo [✓] JRE 检测成功
echo.

REM 检查Edge驱动是否存在
if not exist "%CURRENT_DIR%edgedriver_win64\msedgedriver.exe" (
    echo 错误：未找到 Edge 驱动程序
    echo 期望路径: %CURRENT_DIR%edgedriver_win64\msedgedriver.exe
    echo.
    pause
    exit /b 1
)

echo [✓] Edge 驱动检测成功
echo.


echo.

REM 检查JAR文件
if not exist "%CURRENT_DIR%target\nikon-checkin-1.0.0.jar" (
    echo 错误：未找到 JAR 文件
    echo 期望路径: %CURRENT_DIR%target\nikon-checkin-1.0.0.jar
    echo.
    echo 请先运行以下命令构建项目：
    echo mvn clean package
    echo.
    pause
    exit /b 1
)

echo [✓] JAR 文件检测成功
echo.

echo ========================================
echo 开始执行自动化流程...
echo ========================================
echo.

REM 运行JAR
"%JAVA_EXE%" -jar "%CURRENT_DIR%target\nikon-checkin-1.0.0.jar"

if errorlevel 1 (
    echo.
    echo 程序执行失败，错误代码: %ERRORLEVEL%
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================
echo 程序执行完成！
echo ========================================
pause
