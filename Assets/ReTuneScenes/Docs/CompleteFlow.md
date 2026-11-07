# 完整流程实验版

运行入口：Assets/ReTuneScenes/Scenes/00_Opening.unity。
构建入口同样为 00_Opening；00_Start 保留为标题与返回入口。

## 顺序

开场老街 → 钥匙与继承店铺 → 前任店主的信 → 八音盒 → 标题。
标题 New Game → 邮差 → 吉他手 → 书店老板 → 摄影师。Tape 继续上次保存的场景、顾客、零件和对话页；演奏阶段从该阶段重新开始。
每位顾客：01_Shop 分页对话 → 共鸣原曲 → 02_Parts 选件并上弦 → 03_Performance 演奏 → 04_Reply 回应与离店片段。
摄影师离店 → 05_Ending 投信 → 几天后 → 邮差送达 Michael 的信 → 结束文字 → 返回标题。

## 操作

叙事：Next / Previous，或空格、回车翻下一页。
选件：拖动节奏零件和音色零件；双击查看作用；点击已安装零件移除；点击发条试听判断。
演奏：按住 A/D 修正漂移；蓝色区域正常增长，金色区域 1.8 倍增长；右侧音符按缩圈时机点击。
沿用已确认的持续增长规则，未改成左右联合计分。
Esc 在各场景打开退出确认，使用统一半透明叠层及手写文字按钮 Continue / End。Continue 或再次 Esc 恢复原状态，End 保存已有检查点并退出；编辑器中 End 仅停止运行。返回主菜单保留存档，New Game 才清除旧进度。标题 Review 重看开场；02_Parts 的 Review 回看当前顾客来访时的剧情，不改变游戏进度。

01_Shop 和 04_Reply 的四位人物各有四个表情：首句沿用初始表情，后续新页随机选择另一个表情；回看同页保留该页的选择。只换 Sprite，不调整人物位置、尺寸、父物体或桌面遮挡。邮差新表情的原图偏移通过裁切区域补偿。

## 四套配方

邮差：Steady Wheel + Soft Plate。
吉他手：Sync Wheel + Warm Plate。
书店老板：Sustain Spring + Soft Plate。
摄影师：Echo Wheel + Warm Plate。
错误搭配保留当前阶段，正确搭配进入演奏。

## 叙事与资源

正文位于 Assets/Resources/ReTuneNarrative.json，按完整策划与开场结尾文档扩写为英文草稿。
日期为 Day 1 / 4 / 8 / 12，避免四次来访发生在同一天。
05_Ending 已导入结尾场景素材：室外邮筒 → 人物 → 室内门框，共用原始画布对齐。进入场景后短暂停顿，投信和离开动作按视频每秒 8 帧自动播放一次，保留视频内的停帧，不循环或随字幕翻页重播。UI 与 Opening 共用底部 76 像素字幕条、38 号暖色居中文字、逐字显示及单行分句规则；点击先补全或切换字幕，不显示对白标题、页码或翻页按钮。几天后切到邮差及 Michael 寄件人特写，投递后淡暗并显示结束文字。动画未结束不能切到“几天后”。
吉他手消息无即时回复；相册海报与邮差照片都带 Michael 署名；最后信件同署名。
只有邮差使用正式实验 A/B/C 音轨和固定点击谱面；其他顾客仍使用占位音乐与随机 QTE，漂移参数分别配置。
没有提供制作人员名单，因此结束页暂用感谢文字，未编造人名。

## 编辑与验证

ReTune → Scenes → Connect Complete Story：接入缺失的叙事 UI，保留已有美术；未保存场景会跳过。
ReTune → Scenes → Validate Complete Story：检查七个场景、四套配方、UI 引用及正文高度。
Logs/CompleteStory-validation.txt：Unity 编辑器检查报告。
Logs/CompleteStory-smoke.txt：隔离副本的场景连通测试；演奏进度由测试推进，不代表已验证手感。
