@echo off
chcp 65001 >nul 2>&1
if errorlevel 1 (
    echo 警告: 无法切换到 UTF-8 控制台，输出可能还是乱码
)
set "MAVEN_OPTS=-Dfile.encoding=UTF-8 -Dsun.jnu.encoding=UTF-8"
echo.
echo ========================================
echo  Nikon CheckIn - Selenium自动化程序
echo ========================================
echo.


echo 正在编译项目...
mvn clean compile

echo.
echo 正在启动浏览器...
mvn exec:java


pause
