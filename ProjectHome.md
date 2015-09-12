这个 .NET 小程序会搜索 QQ 游戏目录下的 PKG 资源包和 MIF 图片，并即时浏览资源包的内容和播放 MIF 动画。本程序仅供个人学习用途。

## 软件截屏 ##

![http://qq-game-resource.googlecode.com/svn/trunk/QQGameRes/images/Screen-Shot-v1.3.0.png](http://qq-game-resource.googlecode.com/svn/trunk/QQGameRes/images/Screen-Shot-v1.3.0.png)

## 功能特色 ##

  * 全面格式支持：支持最新的 MIF 压缩存储格式，传统的非压缩 MIF 格式，以及 PKG 资源包格式。

  * 即时动画预览：选中目录或资源包后，会在右侧自动播放其中包含的动画，一览无遗。

  * 灵活导出动画：选中一个动画，点击“导出素材”，即可保存为 Flash SWF 动画格式。另外你也可以选择保存为 SVG 动画格式（需要 FireFox 或 Chrome 才能播放），或者逐帧保存为 PNG 格式。

  * 内存占用更省：所有预览的图片和动画都在内存中压缩存储，比原始文件的尺寸节省 30%-70%。

  * 系统界面整合：使用 Windows 风格的目录和预览区，样式美观，操作简便。

  * 界面响应更佳：耗时操作都在后台处理，用户操作毫无停顿。

## 运行要求 ##

版本 1.3 要求安装了 .NET 4.0 的 Windows 系统。

版本 1.1 要求安装了 .NET 2.0 的 Windows 系统。

如果在安裝了 QQ 游戏的电脑上运行，程序会自动找到 QQ 游戏目录并列出相关资源。如果没有安装 QQ 游戏，或者程序没能自动找到，则需要手动指定 QQ 游戏的目录或资源文件。

## 参考资料 ##

PKG 文件格式的解析在[这里](http://www.cppblog.com/tx7do/archive/2010/02/24/108364.html)。

MIF 文件格式的解析在[这里](http://umu618.wordpress.com/2006/04/02/%E7%BB%88%E4%BA%8E%E6%8A%8A-qqgame-%E7%94%A8%E7%9A%84-mif-%E6%A0%BC%E5%BC%8F%E7%9A%84%E5%9B%BE%E7%89%87%E7%A0%94%E7%A9%B6%E9%80%8F%E4%BA%86/)。作者并没有提供源代码，但是提供了可执行文件供[下载](http://umu.ys168.com/)。

页面左上角的项目图标来自[这里](http://www.iconarchive.com/show/oxygen-icons-by-oxygen-icons.org/Actions-document-preview-archive-icon.html)。