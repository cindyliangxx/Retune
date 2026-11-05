# Re:Tune 按 Excel 重排的完整游戏流程

依据：用户提供的《文本.xlsx》Sheet1 A1:C143，以及用户已确认的开场、三段结尾、手机、相册与签名演出。
状态：本文件是接入方案与逐句核对稿；没有替换当前 Unity 中的旧扩写对白，也没有拆分美术素材。工作簿保持原样。

## 1. 顺序与场景

开场分镜 `00_Opening` → 标题 `00_Start` → 邮差完整流程 → 吉他手完整流程（离店后手机）→ 书店老板完整流程（调律后对话中展示相册）→ 摄影师完整流程 → 正式结尾 `05_Ending` → 制作名单与返回标题。

四位顾客都复用：`01_Shop` 调律前对话与共鸣 → `02_Parts` 选件上弦 → `03_Performance` 演奏 → `04_Reply` 调律后对话。只有第四位离店后进入 Ending。手机留在第二位的离店段，相册留在第三位的调律后对话中，不另当作结尾。

Excel 中有 123 条人物对白，另有 1 条手机短信。调律前分别为 18 / 17 / 19 / 21 条，调律后分别为 12 / 10 / 13 / 13 条。行数不等于画面点击次数：较长对白可以按完整语义分成显示段，但不能漏掉原句。

## 2. 说话人与文本处理

- Excel 的 `Owner` 是玩家经营 Re:Tune 的店主，界面显示 `You`，沿用浅青色。
- `Bookshop Owner` 才是第三位顾客书店老板，界面显示 `Bookshop Owner`，沿用顾客暖金色标签。不能把这两类合并。
- `Postman`、`Guitarist`、`Photographer` 分别是另外三位顾客；摄影师的作品署名与寄信署名必须统一为 `Michael`。正文不额外添加自我介绍。
- A 列空白、B 列有文本的行沿用上一位说话人。例如第 10、11 行仍由邮差说，第 15 行仍由玩家说。
- 第 19、52、87、127 行 B 列里的“（共鸣音效）”是演出提示，转为音乐事件，不作为英文对白显示。
- C 列用于中文校对，游戏显示 B 列英文。表头、调律前后标签、注意事项和画面说明不作为人物对白。
- 全部对话沿用下方半透明悬浮框与逐字显示。第一次点击补全文字，下一次点击继续；音量滑条和 Title 不触发翻页。
- Excel 未规定 Day 1 / 4 / 8 / 12 的日期；当前项目的日期只是旧版过渡设定，不应误写成这份表的内容。

## 3. 四位顾客的具体流程

### 第一位：邮差

1. `01_Shop`：依次播放第 3–18 行。从犹豫进门、得知自己是第一位顾客，到解释误投信件、反复检查与担心再犯错，玩家邀请他触碰八音盒。
2. 第 18 行结束后触发共鸣，播放焦虑原曲，再显示第 19 行的惊讶反应。继续第 20 行玩家回应。
3. 第 20 行结束，进入 `02_Parts`。沿用拖动零件、上弦试听、判断搭配的玩法；正确结果进入 `03_Performance`。
4. 演奏成功，进入 `04_Reply`，完整播放第 22–33 行。不是立即“治好焦虑”，而是愿意只认真检查一次、尝试相信自己。
5. 第 31 行提出去街角书店看看；第 33 行祝 Re:Tune 生意顺利，邮差离店，随后吉他手到访。

美术：邮差立绘、店内背景、现有八音盒与玩法素材即可。Excel 没有要求额外拍一段邮差站在书店前的动画；相册右侧照片承担这个视觉呼应。

### 第二位：吉他手

1. `01_Shop`：第 37–51 行。讲述与搭档在即兴和固定编排上的冲突，他暂时不愿让步。
2. 第 51 行结束触发共鸣，播放冲突原曲后显示第 52 行，再继续第 53 行。
3. 接 `02_Parts`、`03_Performance`，成功后回到 `04_Reply`。
4. 第 55–64 行。他听见两声部开始配合，回忆起曾经即兴的乐趣；仍然生气，但愿意重新听旧录音。
5. 第 64 行说完感谢、人物离店后，按第 65–66 行切入手机画面。
6. 手机屏幕显示原表短信：`Wanna try that improvised set you were talking about next time?`。点击独立 `SEND` 按钮，轻微下压并给出 `Sent` 提示，停留约 1–2 秒，点击继续到书店老板。发送演出是建议实现，不是 Excel 新增分支。

手机使用已提供的整机图和独立 SEND 图层；同画布同坐标叠放，不拆机身。现有屏幕已画好短信，不另画第二份重叠文字。不添加对方回信，不把手机段留到游戏最后。

### 第三位：书店老板

1. `01_Shop`：第 70–86 行。她认识前店主，经营书店二十多年，即将关门；担心曾经的事情会失去意义。
2. 第 86 行后触发共鸣，显示第 87 行的孤独感受，再继续第 88 行。
3. 接 `02_Parts`、`03_Performance`，成功后进入 `04_Reply`。
4. 第 90–94 行。她承认仍然伤心，也开始想起书店曾经发生的事情，提出展示回忆展照片。
5. 第 94 行完整结束后，按第 95 行显示相册。这个插入点在调律之后，不能放到初次进门阶段。
6. 相册整图淡入，暂时隐藏立绘与常规对话框。左边是吉他手与搭档的演出照片，右边是邮差看书店的照片，两边都有 Michael 署名。相册让前两位顾客的故事在此相连。
7. 有无字照片与独立签名图层时，可让左、右签名依次手写显现；只有当前完整图时先整图展示。不可直接把完整图原签名留着再叠一份动画。
8. 看完相册，点击回到店内对话，接第 96 行玩家的 `Wow, meaningful !`，再完整播放第 97–103 行。她决定把记忆分享出去，而不是将它们全部收起来。
9. 第 103 行感谢并离店，然后摄影师到访。

基本交互只需打开、观看、关闭相册。照片点击放大是可选增强，不需要另做寻物任务或强制两张都点一次。当前相册参考带绘图软件网格外框，正式接入需要干净导出的相册画面。

### 第四位：摄影师

1. `01_Shop`：第 108–126 行。他曾给前任店主和老街拍照，与老朋友约定旅行制作相册，却因生活耽搁多年，信也一直没寄。
2. 第 126 行结束触发共鸣，显示第 127 行，再继续第 128 行。
3. 最后一次 `02_Parts`、`03_Performance`，成功后进入 `04_Reply`。
4. 第 130–137 行。第二声部像有人回应；他想起朋友拍照时吹口哨，意识到不必再等“完美时机”。
5. 第 138 行决定把相机包里的信寄出去。继续第 139–142 行：希望得到回音、嘱托玩家照顾店铺、玩家答应。
6. 第 142 行结束，摄影师离店，接表尾第 143 行“接收场剧情”。这里承接用户明确的正式结尾，不再转到下一位顾客。

不要把第三位的展览相册自动当成摄影师未完成的旅行相册：两者是不同物件。也不要添加 Excel 没有写的朋友回信或两人重逢。

## 4. 最后的 Ending

结尾三段来自用户另外确认的结尾描述，Excel 只提供进入这个段落的衔接，不包含完整结尾台词。

1. 寄出信件：摄影师在街边邮筒前拿着信，停顿约 2 秒；点击后把信投进邮筒。信封通过投信口前沿遮挡消失。
2. 几天后：转场到送信画面，淡入 `A few days later`，停留约 2 秒再淡出。也可以单独黑屏显示，但建议少一次打断。
3. 信件抵达：邮差从手中信件取出一封；镜头特写寄件人 `Michael`，呼应相册签名；随后把信投入收件人信箱。
4. 收尾：周围画面渐暗，保留信箱，制作名单浮现，最后显示 `Return to Title`。未收到的制作人员名单保留待填，不虚构人名。

最少素材：寄信街景、摄影师拿信的手、独立信封、街边邮筒及投口遮挡；送信街景、邮差与手中信件、独立目标信封、收件人信箱及投口遮挡；信封特写和 Michael 署名。已提供素材先直接使用，新增拆层由美术导出，不在本次梳理中拆图。

## 5. 音乐与现有玩法

开场第 9 个 Music Box 分镜开始 LoFi BGM；日常对话、手机、相册、结尾可延续。02/03 使用玩法音乐时暂停 BGM，回到其他场景恢复。四次共鸣原曲的触发点必须分别对齐 Excel 第 19/52/87/127 行；共鸣试听时建议暂时压低或暂停 LoFi，避免遮盖原曲，这属于接入时需要补的音频互斥。

沿用现有选件、上弦、AD 修正与点按 QTE，不因换对白增加新的评分条件。手机发送和相册观看用于叙事推进，不影响配方正确与否，也不增加失败条件。玩法失败沿用当前关卡重试，不重播整位顾客的开场对白。

## 6. 与当前项目的差异与接入顺序

当前 `ReTuneNarrative.json` 是此前的英文扩写草稿，四位顾客合计 82 段，不是此 Excel 的逐句版本。本次没有把“已梳理”标为“已接入”。

建议接入顺序：

1. 从 B 列导入四位顾客的调律前后正文，保留 A 列说话人和每条源行号；空白说话人继承上一条。
2. 明确 Owner=You、Bookshop Owner=第三位顾客，修正当前旧对白解析里把 Owner 视为书店老板的规则。
3. 把四个共鸣标记转为独立事件，触发时先音乐后反应，不把中文括号显示为对白。
4. 第二位第 64 行后挂手机事件；第三位第 94 行后挂相册事件，结束后恢复第 96 行。
5. 第四位第 142 行之后只进入正式 Ending，按三段演出播放。
6. 对照下面完整对白逐句检查：123 条对白、1 条短信、4 个共鸣点、1 个手机插入点、1 个相册插入点及 1 个结尾入口。

以下行号对应源 Excel 的 Sheet1；英文保留原表，括号音效属于事件提示。标点与空格如需润色，应单独校对，不在流程梳理时擅改对白。

## 7. 逐句对白核对

### 邮差 / Postman

#### 调律前

- **Postman（B3）**：Uh, hi. Is this... Re:Tune?

- **You（B4）**：Yep, that's us. Come on in.

- **Postman（B5）**：Oh, good. I actually walked past twice. Wasn't sure if you were open.

- **You（B6）**：I just took over, actually. You're my first customer.

- **Postman（B7）**：Well... I heard you help people with their feelings. I thought I'd have a try.

- **You（B8）**：OK.  What's on your mind?

- **Postman（B9）**：It's probably gonna sound stupid, but... I messed up at work.

- **Postman（B10）**：I'm a postman. Just started a few weeks ago. And on my first day, I delivered a letter to the wrong house.

- **Postman（B11）**：I sorted it out in the end. But ever since then, I just can't stop checking everything. The house number, the street name...

- **You（B12）**：That sounds exhausting.

- **Postman（B13）**：It is. I just... I really don't wanna mess up again.

- **You（B14）**：Well, we could give something a try, if you're up for it.

- **You（B15）**：See that music box? Just rest your hand on the lid. It's supposed to play something that reflects how you're feeling.

- **Postman（B16）**：Wait, seriously? That's... actually a way?

- **You（B17）**：Shall we give it a try？

- **Postman（B18）**：Well, I've tried just about everything else. Might as well give it a shot.

- **Postman（B19）**：（共鸣音效）...Oh. Wow. That's... pretty frantic, isn't it?

- **You（B20）**：Yeah, sounds anxious. Let's see if we can help it settle down a little.

#### 调律后

- **Postman（B22）**：Huh... That's different.

- **You（B23）**：How do you feel now?

- **Postman（B24）**：I don't know how you did that, but... it feels quieter in my head.

- **Postman（B25）**：I mean, I'm still gonna worry about getting things wrong. But maybe I don't have to worry quite so much.

- **You（B26）**：You can be careful without being so hard on yourself.

- **Postman（B27）**：Yeah. I guess one mistake doesn't mean I'm terrible at my job.

- **Postman（B28）**：Maybe tomorrow I'll just check each address once. Properly. And then... trust myself a little.

- **You（B29）**：Sounds like a good start.

- **Postman（B30）**：Thanks. I really wasn't sure about this place when I walked in.

- **Postman（B31）**：Think I'll stop by that bookshop on the corner before my next round. Haven't had a proper look in ages.

- **You（B32）**：Take your time. And good luck out there.

- **Postman（B33）**：Thanks. Oh, and... good luck with the Re:Tune, too!

### 吉他手 / Guitarist

#### 调律前

- **Guitarist（B37）**：Hey. This is Re:Tune, right?

- **You（B38）**：That's right. What can I do for you?

- **Guitarist（B39）**：Heard this place was open again. People around here have been talking about it.

- **You（B40）**：Well, I'm glad you came by. What's going on?

- **Guitarist（B41）**：Ugh. It's my partner. We've been playing together for years, and yesterday we had a fierce argument.

- **Guitarist（B42）**：He keeps wanting to improvise. Change the rhythm, mess around with the set... right in the middle of a song!

- **Guitarist（B43）**：I mean, come on. We spend weeks practicing, and then he just throws the whole thing out the window?

- **You（B44）**：Sounds like you two see things pretty differently.

- **Guitarist（B45）**：Yeah, no kidding. He says I'm too obsessed with getting everything right and it may bore our audience.

- **Guitarist（B46）**：And I got mad. Really mad. Said some things I probably shouldn't have.

- **You（B47）**：Do you think there's any truth to what he said?

- **Guitarist（B48）**：...I don't know. That's kind of why I'm here. And for now, I'm not backing down.

- **You（B49）**：Fair enough. This music box may help you figure out what you're really thinking.

- **You（B50）**：Please place your hand on the lid.

- **Guitarist（B51）**：OK...

- **Guitarist（B52）**：（共鸣音效）...Well, that sounds about right. Like two people trying to play over each other.

- **You（B53）**：Let's see if we can get them to play together.

#### 调律后

- **Guitarist（B55）**：Huh. Okay... I wasn't expecting that.

- **You（B56）**：Better?

- **Guitarist（B57）**：Yeah. It's still got some kick to it. But now the two parts actually work together.

- **Guitarist（B58）**：Actually, we used to mess around with songs all the time. Half our best stuff came from that.

- **Guitarist（B59）**：Guess somewhere along the way, I got so worried about messing up that I stopped enjoying it.

- **You（B60）**：Maybe it's worth hearing him out.

- **Guitarist（B61）**：Yeah. I'm still annoyed with him, though. Not gonna pretend I'm not.

- **Guitarist（B62）**：But... maybe I'll listen to some of our old recordings again. See what I've been missing.

- **You（B63）**：Hope it works out.

- **Guitarist（B64）**：Yeah. Me too. Thanks, seriously.

#### 离店后手机（A65–C66）

点击 SEND 发送：

> Wanna try that improvised set you were talking about next time?

随后进入书店老板的调律前对话。

### 书店老板 / Bookshop Owner

#### 调律前

- **Bookshop Owner（B70）**：Hello there. Hope I'm not interrupting anything.

- **You（B71）**：Not at all. Please come in.

- **Bookshop Owner（B72）**：Thanks. I run the bookshop around the corner. You might've seen it.

- **You（B73）**：Oh, yeah! I walk past it every day.

- **Bookshop Owner（B74）**：I've been meaning to stop by. Used to know the previous owner of this place.

- **Bookshop Owner（B75）**：But I never thought I'd come here looking for help.

- **Bookshop Owner（B76）**：Anyway... I'm closing the bookshop soon. After more than twenty years...

- **You（B77）**：Oh... I'm sorry to hear that.

- **Bookshop Owner（B78）**：I knew this day would come eventually.

- **Bookshop Owner（B79）**：It's just... You spend so long in one place, you start thinking it'll always be there...

- **You（B80）**：Twenty years is such a long time.

- **Bookshop Owner（B81）**：It is. I've watched so many kids grow up. Some of them even come back with their babies now.

- **Bookshop Owner（B82）**：And I keep thinking... once I lock that door for the last time, will any of it matter anymore?

- **You（B83）**：I believe it means a lot to you.

- **Bookshop Owner（B84）**：Yeah. More than I realised, I think... These days, the sadness keeps me awake most nights.

- **Bookshop Owner（B85）**：May I try that magic music box? I've heard things about it.

- **You（B86）**：Of course. Just put your hand on the lid.

- **Bookshop Owner（B87）**：（共鸣音效）...Huh. That reminds me of the shop after everyone's gone home.

- **You（B88）**：Let's see what we can do.

#### 调律后

- **Bookshop Owner（B90）**：That's lovely... Still a little sad, though.

- **You（B91）**：I guess that's alright. You don't have to stop being sad about it.

- **Bookshop Owner（B92）**：You know, I've been so busy thinking about the shop closing, I almost forgot how much happened while it was open.

- **Bookshop Owner（B93）**：Oh! Actually, let me show you something.

- **Bookshop Owner（B94）**：I'm planning a memory exhibition. These are some of the photos that'll be on display.

**[画面事件，A95] 此处展示相册，观看结束后继续第 96 行。**

- **You（B96）**：Wow, meaningful !

- **Bookshop Owner（B97）**：I've got years of photos, old notes, little things people left behind.

- **Bookshop Owner（B98）**：I was just going to put them away, but... maybe I should show them to more people.

- **You（B99）**：That sounds wonderful. I'd love to come.

- **Bookshop Owner（B100）**：Really? Well, see you then!

- **Bookshop Owner（B101）**：I'm still going to miss the place. But maybe closing the shop doesn't mean losing everything that happened there.

- **You（B102）**：I'm sure a lot of people will be happy to see those memories.

- **Bookshop Owner（B103）**：I hope so. Well then, I'd better get things ready. Thank you, dear.

### 摄影师 / Photographer

#### 调律前

- **Photographer（B108）**：Ah, hello there. So you're the one running this place now?

- **You（B109）**：Hello. Welcome to Re:Tune.

- **Photographer（B110）**：Good to see it open again. I used to take photos of the previous shopkeeper and the shop.

- **You（B111）**：Oh, are you from around here?

- **Photographer（B112）**：More or less. Been taking pictures of this old street for longer than I care to remember.

- **Photographer（B113）**：Funny, isn't it? Same little street, all these different lives passing through.

- **You（B114）**：Sounds like you've got a lot of stories.

- **Photographer（B115）**：Oh, plenty. Though there's one I've been thinking about a lot lately.

- **Photographer（B116）**：I have an old friend. We used to go everywhere together, cameras in hand. Always planning our next big adventure.

- **Photographer（B117）**：We even made a deal. One day we'd go on a proper trip, take a whole bunch of photos, and make an album together.

- **You（B118）**：That sounds nice. Did you ever go?

- **Photographer（B119）**：...No. Never quite got around to it.

- **Photographer（B120）**：Life got in the way, I suppose. Work, family, all the usual things. We wrote to each other less and less.

- **Photographer（B121）**：And before I knew it, years had gone by. I've still got that half-empty album sitting at home.

- **You（B122）**：Have you thought about getting back in touch?

- **Photographer（B123）**：Oh, more times than I can count. Even wrote him a letter once. Never sent it, though.

- **Photographer（B124）**：After all this time, I wouldn't even know what to say.

- **You（B125）**：I see... Well, would you like to try the music box?

- **Photographer（B126）**：Ah, I know it. Rest my hand on it, right?

- **Photographer（B127）**：（共鸣音效）...My goodness. That sounds rather lonely, doesn't it?

- **You（B128）**：Yeah... Let's see if we can bring something else into it.

#### 调律后

- **Photographer（B130）**：It's beautiful. That second tune... it sounds like someone's answering.

- **You（B131）**：How is it?

- **Photographer（B132）**：You know, my friend used to whistle while we were taking photos. Drove me crazy sometimes.

- **Photographer（B133）**：I'd be trying to get the perfect shot, and there he'd be, whistling away without a care in the world.

- **Photographer（B134）**：... I'd give anything to hear that again.

- **You（B135）**：You really miss him, don't you?

- **Photographer（B136）**：I've spent all these years waiting for the right moment to get back in touch. But... I don't suppose there's ever a perfect moment.

- **You（B137）**：Maybe not. But that doesn't mean it's too late to try.

- **Photographer（B138）**：... You know what? I think I'll finally send that letter. It's been sitting in my camera bag long enough.

- **You（B139）**：I hope you hear from him.

- **Photographer（B140）**：Thank you. And... take good care of this little shop, won't you?

- **You（B141）**：I will. Promise.

- **Photographer（B142）**：Good. I have a feeling you're going to help a lot of people here.
