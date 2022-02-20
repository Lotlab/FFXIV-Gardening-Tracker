# FFXIV Gardening Tracker

最终幻想14园艺时钟

此插件依赖于FFXIV解析插件和OverlayPlugin悬浮窗插件，请确保实现安装了这两个插件。

## 使用说明

1. 下载后解压，使用ACT载入插件。
3. 载入插件后，请重新加载OverlayPlugin（取消选中插件的Enable复选框并重新选中）
3. 确认载入顺序，保证此插件在OverlayPlugin之后。（即，保证此插件在OverlayPlugin和FFXIV解析插件的下方。此行为是默认值）
4. 调整FFXIV解析插件的设置，确保解析模式为`WinPcap`模式。**选择其他模式可能无法正确的处理交互信息，导致无法记录对土地的操作。**
   - 如果之前没有使用过此模式，则可能需要安装[npcap](https://npcap.com/#download)（[下载直链](https://npcap.com/dist/npcap-1.60.exe)）
5. 尝试护理植物，若能够正确记录则说明安装正确。
6. 在OverlayPlugin内点击**新建**按钮新建悬浮窗，预设选择**园艺时钟**，点击添加即可。

### 常见问题

1. 为什么我的操作没有被记录？

   请按照上面说明修改FFXIV解析插件的设置。

2. 为什么我无法找到名为园艺时钟的悬浮窗预设？

   请尝试重新加载OverlayPlugin。

3. 为什么插件加载失败，提示数据文件未找到？

   请保证完全解压了压缩包，并且压缩包内的其他文件正确的与加载的插件dll位于同一文件夹下。

### 已知问题

1. 在房区内下线后上线无法正确识别当前区域。请保证在房区内下线后重新切换地图（包括进入房屋、传送到其他地图等）再对园圃操作。

## 悬浮窗集成

### 日志行文档

日志行头：`00|[Time]|0|GardeningTracker|[Type]|`

#### 00: 园圃操作

```
|<ServerID>|<TerritoryID>|<WardNum>|<LandID>|<ObjectID>|<HousingLink>|<Operation>|<Param1>|<Param2>||
```

- HousingIdent: 房屋信息，Hex格式，8Bytes。数据排布与Sappine的LandIdent一致
  - LandID: 房屋ID
  - WardNum: [0-5]房区ID, [6-15]公寓房间编号
  - TerritoryID: 区域ID
  - ServerID: 服务器ID
- ObjectID：物体ID，Hex格式。请参阅`HousingFurniture.csv`和`EObjName.csv`
- HousingLink：家居位信息，Hex格式，4Bytes
  - [0-15] ObjIndex: 家具ID
  - [24-31] SubIndex: 家具子ID
- Operation：操作
  - 0：播种
  - 1：护理
  - 2：施肥
  - 3：收获
  - 4：处理
- Param1：参数1。施肥则为肥料ID，播种则为土壤ID
- Param2：参数2。播种为种子ID

#### 01：园圃操作（文本）

```
|<GardeningIdent>|<Operation>|<Param1>|<Param2>||
```

- GardeningIdent：园圃位置
- Operation：操作
- Param1：参数1。施肥则为肥料名，播种则为土壤名
- Param2：参数2。播种为种子名

### OverlayPlugin 集成文档

#### onGardeningDataChange

OverlayListener。园圃数据变动后下发园圃数据用。

#### onGardeningZoneChange

OverlayListener。当前区域变动后触发。

#### RequestGardeningData

OverlayHandler。请求更新园圃数据，会返回园圃数据。

