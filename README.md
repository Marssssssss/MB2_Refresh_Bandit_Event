## 简介

骑砍2是个好游戏，玩的时候我发现这个游戏里的匪盗军团似乎有点弱了，尤其玩家中后期的时候匪盗基本没有什么来自匪盗的压力。为了增强中期可玩性，我将匪盗这个中立势力进行了一下加强。

当然我不是直接加强，加人数 or 加军团数等等，而是包装了一个类似事件的内容。每隔一段时间匪患就会来临，期间匪徒军团数量暴增，且每天都会增加。于是能看到一地匪盗的画面，做完以后看还怪有意思的嘞~

早期版本刷新人数，刷新频率都是写死的，后续会加到可配置的选单中处理。

游戏各个官方 `mod` 版本需求为：

- `Native v1.2.8`
- `SandBoxCore v1.2.8`
- `Sandbox v1.2.8`
- `CustomBattle v1.2.8`
- `StoryMode v1.2.8`



## 使用方式

项目包含源码（`RefreshBanditEvent`）和直接可用的 `dll`（`Debug` 模式下编的...后面会再调整下工作流）。

源码是 `Visual Studio 2022` 工程，如果需要编译的话可以参照 [Modding文档](https://docs.bannerlordmodding.com/_tutorials/basic-csharp-mod.html#preparation) 里的 ` Setting up your Project` 部分配置项目依赖的 `dll` 和生成路径。然后进行生成。

无论是使用现成编译好的 `dll` 还是自己编，最后一步就是到游戏目录的 `Modules` 下，创建一个名为 `RefreshBanditEvent` 的目录，然后将下面两个文件 or 目录放到里面：

- `SubModule.xml`
- `bin`



## TODO

- 调整 `mod` 工作流
- 添加配置页面，使事件发生频率、刷新频率、刷新人数和刷新人数上限可配置
- 添加多样性，考虑先加入 `Boss` 匪盗军队、精英军队等等
