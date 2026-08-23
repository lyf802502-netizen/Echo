# Echo

> 世界从未安静过。<br>
> 每一种情绪，都有它的频率。<br>
> 愤怒是低沉的轰鸣，悲伤是绵长的呜咽，焦虑是无序的杂音……<br>
> 当这些声音汇聚在一起，就成了这座城市的底色。<br>
> 而我们，是一支特殊的乐队。<br>
> 我们的工作，是为这个失谐的世界，重新调音。<br>

## 游戏内容展示

<p align="center">
  <img src="https://github.com/user-attachments/assets/fe269071-392a-4ae3-8795-b59b51ba4017" width="48%" title="开始画面" alt="开始画面"/>
  &nbsp;&nbsp;&nbsp;&nbsp;
  <img src="https://github.com/user-attachments/assets/b00156a5-0645-4c46-8f9a-4d6c395e34e5" width="48%" title="剧情画面" alt="剧情画面"/>
</p>

## 游戏简介

本作是一款以 “声音与情绪” 为主题的视觉小说。在游戏中，玩家将跟随一支特殊乐队，在城市失调的声音中调查事件、推进章节并逐步揭开角色之间的关系。对话、立绘、背景、视频与音效共同构成叙事场景；此外在关键剧情节点，玩家还需要完成与故事绑定的节奏关卡，在演奏音乐的过程中带来回响，为这个世界 “重新调音”。

## 核心特性

- **视觉小说叙事**：基于 CSV 剧本驱动对话、说话人、角色立绘、选项和场景切换，支持历史记录、回放与打字机效果。
- **剧情与节奏玩法联动**：剧本通过 `playrhythm` 命令进入节奏场景，歌曲结束或失败后自动回到正确的剧情行，保持叙事连续性。
- **可配置的节奏判定**：使用 Koreographer 读取谱面事件，支持多轨道、普通音符与长按音符，并根据 Perfect / Great / Miss 统计连击、准确率和得分。
- **完整的进度系统**：提供章节解锁/完成、手动存档、继续游戏槽位和剧情节点自动存档，适合多周目体验。
- **可扩展的内容收集**：内置 CG、音乐和场景画廊，以及角色选择、章节选择和设置界面，方便持续扩充剧情内容。

## 技术栈与架构亮点

- **引擎与语言**：Unity 2023.1.0f1、C#、TextMeshPro、Unity Input System。
- **模块化目录**：`GameStart`、`Shared`、`Rhythm/Runtime` 与 `Integration/VNovelizer` 按职责拆分，节奏玩法和视觉小说系统可以独立调试与复用。
- **数据驱动内容**：`Resources/VNovelizerRes/VNScripts/*.csv` 管理剧情，Koreography 资源管理节奏谱面，新增章节或歌曲无需修改核心流程代码。
- **事件驱动会话控制**：`RhythmSessionController` 通过分数、生命、连击、暂停、完成/失败等事件向 HUD 和剧情桥接层广播状态，降低模块耦合。
- **对象池与场景恢复**：音符和命中特效使用对象池减少运行时 Instantiate/Destroy；跨场景流程保存脚本名与行 ID，回到视觉小说后由 `VNManager` 正式恢复剧情状态。
- **可配置的故事/独立模式**：节奏会话支持 Story 与 MusicGame 两种模式，可分别控制扣血、分数、HUD 显示及歌曲结束行为。

## 如何运行本项目

### 环境要求

- **Unity Editor**：`2023.1.0f1` 或更高版本
- **依赖**：TextMeshPro、Input System（Unity 内置）；项目已包含 Koreographer 运行时插件
- **代码编辑器**：Visual Studio 2022、Rider 或 VS Code

### 启动步骤

1. **克隆项目**
   ```bash
   git clone https://github.com/lyf802502-netizen/Echo.git
   ```
2. **打开工程**：在 Unity Hub 中选择克隆后的 `Echo` 文件夹。
3. **进入游戏**：打开 `Assets/Scenes/GameStartScene.unity`，点击 Play。
4. **调试入口**：视觉小说主菜单、角色选择、章节选择和节奏场景均已配置在 `Assets/Scenes/` 中，可按需直接运行。

## 关于作者

- **开发者**：小林同学（Lin_Catom）
- **求职意向**：游戏客户端开发 / Unity 开发
- **联系方式**：lyf502802@163.com

## 资源致谢

- Unity、TextMeshPro、Input System
- Koreographer（Sonic Bloom）
- VNovelizer 视觉小说框架及项目内美术、音乐资源
