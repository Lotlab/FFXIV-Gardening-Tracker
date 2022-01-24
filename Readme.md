# |FFXIV Gardening Tracker

最终幻想14园艺时钟

## 日志行文档

日志行头：`00|[Time]|0|GardeningTracker|[Type]|`

### 00: 园圃操作

```
|<HousingIdent>|<ObjectID>|<HousingLink>|<Operation>|<Param1>|<Param2>||
```

- HousingIdent: 房屋信息，Hex格式，8Bytes。数据排布与Sappine的LandIdent一致
  - [0-15] LandID: 房屋ID
  - [16-21] WardNum: 房区ID
  - [22-31] RoomNum: 公寓编号
  - [32-47] TerritoryID: 区域ID
  - [48-64] ServerID: 服务器ID
- ObjectID：物体ID。请参阅`HousingFurniture.csv`和`EObjName.csv`
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

### 01：园圃操作（文本）

```
|<GardeningIdent>|<Operation>|<Param1>|<Param2>||
```

- GardeningIdent：园圃位置
- Operation：操作
- Param1：参数1。施肥则为肥料名，播种则为土壤名
- Param2：参数2。播种为种子名

### 02：当前数据

```
|<Content>|
```

是当前记录的JSON数据。