Loading Scene:

\- 玩家进入游戏，出现游戏logo，然后进入main scene



Main Scene:

\- Main Scene 展示有两个属性：金币和skill值

\- 上下左右拖拽：旋转视角（水平自由，俯仰30-80°限制）

\- view mode:

  - 加载人物、车和模型

  - 左右两个button切换套装（人物、车和场景），有loading时间

  - 右下角：play button（未解锁时锁定不可点击）

  - 按住play button → 进入map select scene

  - 上下左右拖拽：旋转视角（水平自由，俯仰30-80°限制）

  - decoration button → 进入deco mode



\- Deco Mode:

  - 保留人物、车和场景 和旋转场景

  - 返回view mode button

  - dress up button:

    - 返回和dress up button 和 modify car button消失

    - 左侧弹出人物服装list（type: 上装、下装、头发），一排3个item的preview，可以scroll，带收回按钮 （点收回按钮，list弹回去，三个button恢复，相机恢复）

    - 人物默认有一套基础穿搭（上装、下装、头发），在list了，里面高亮

    - 已购买 → 点击弹出浮窗是否wear(yes/no) yes替换scene里面对应type，并且高亮这个item

    - 未购买 → 点击弹出浮窗 显示价格，是否buy（yes/no），

    - yes → 扣钱并替换，替换scene里面对应type，并且高亮这个item；钱不够 → 显示提示2s消失；no → 浮窗消失



  - modify car button:

    - 下方弹出改装件list（尾翼、轮毂、皮肤），一排3个item的preview，可以scroll，带收回按钮 （点收回按钮，list弹回去，三个button恢复，相机恢复）

    - 汽车默认有基础改装件（尾翼、轮毂、皮肤），在list了，item preview高亮

    - 相机聚焦汽车，画面整体上移

    - 同服装逻辑：buy确认、钱不够提示





Map Select Scene:

\- 加载一个大的3D地图模型

\- 旋转视角：水平自由、俯仰10-80°限制

\- 地图坐标点：在这个大的3D大地图上面有一些坐标点，每个坐标点其实对应了一个游戏的场景scene，坐标点上面会有一个billboard放置图片，billboard始终面朝着相机并且垂直地面，billboard可以被选择其中的一个（其中一个是）

\- play button → 进入选中的billboard所对应的坐标点所对应的scene开始游戏

Play Scene:

\- 加载选择的人和车（所有item）→ 进入赛车游戏

\- 游戏结束 → 结算界面

  - 三个button：重玩、回到map select scene、回到main scene

