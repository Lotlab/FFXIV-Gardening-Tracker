using Lotlab.PluginCommon;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using GardeningTracker.Packets;
using System;

namespace GardeningTracker
{
    class HybridStats : PropertyNotifier
    {
        Config config { get; }

        SimpleLogger logger { get; }

        GardeningData data { get; }

        HttpClient client { get; }

        bool Enabled => !string.IsNullOrEmpty(config.StatsWebhookUrl);

        public HybridStats(SimpleLogger logger, GardeningData data, Config cfg) 
        { 
            this.config = cfg;
            this.logger = logger;
            this.data = data;

            client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
        }

        public void UploadTestData()
        {
            _ = postDataAsync(new HybridResult(config.StatsUserName, "测试种子", "杂交失败"));
        }

        public void UploadResult(HarvestResult result)
        {
            var seedID = data.GetSeedIdByIndex(result.Result1Seed);
            var seedName = data.GetSeedName(seedID);
            var product1 = data.GetItemName(result.Result1ID);
            var product2 = data.GetItemName(result.Result2ID);
            if (product2 != null && result.Result2Count > 0)
            {
                logger.LogInfo($"{seedName} 产物: {product1}*{result.Result1Count}, {product2}*{result.Result2Count}");
            }
            else
            {
                logger.LogInfo($"{seedName} 产物: {product1}*{result.Result1Count}");
            }

            if (!Enabled)
                return;

            var hybridProduct = data.GetSeedProductID(seedID) == result.Result1ID ? product2 : product1;
            if (hybridProduct == null)
                hybridProduct = "杂交失败";

            _ = postDataAsync(new HybridResult(config.StatsUserName, seedName, hybridProduct));
        }

        const string tokenHeaderName = "AirScript-Token";

        async Task<bool> postDataAsync(HttpContent content)
        {
            if (client.DefaultRequestHeaders.Contains(tokenHeaderName))
                client.DefaultRequestHeaders.Remove(tokenHeaderName);

            client.DefaultRequestHeaders.Add(tokenHeaderName, config.StatsWebhookToken.Trim());

            try
            {
                var resp = await client.PostAsync(config.StatsWebhookUrl.Trim(), content);
                var respBody = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    logger.LogError($"服务器返回 {resp.StatusCode.ToString()}，请检查上报地址、令牌是否有误。");
                    logger.LogDebug(respBody);
                    return false;
                }
                if (!respBody.StartsWith("{"))
                {
                    logger.LogError($"服务器返回数据非JSON格式，请检查上报地址是否有误。");
                    return false;
                }

                try
                {
                    var obj = JsonConvert.DeserializeObject<KDocsResponse>(respBody);
                    if (obj.status != "finished" || obj.error != "")
                    {
                        logger.LogError($"脚本执行错误：{obj.error}");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError($"服务器返回数据反序列化失败，{e.Message}");
                }
            }
            catch (Exception e)
            {
                logger.LogError($"请求异常：{e.Message}");
            }
            return true;
        }

        async Task<bool> postDataAsync(HybridResult result)
        {
            var content = new JsonContent(new KDocsContext<HybridResult>(result));
            logger.LogDebug(content.Text);
            logger.LogInfo("准备上报数据");

            for (int i = 0; i < 3; i++)
            {
                var success = await postDataAsync(content);
                if (success)
                {
                    logger.LogInfo("杂交结果上报成功");
                    return true;
                }

                logger.LogInfo($"{i+1}秒后重试");
                await Task.Delay((i + 1) * 1000);
            }

            logger.LogInfo("杂交结果上报失败");
            return false;
        }
    }

    struct HybridResult
    {
        public string seed;
        public string name;
        public string result;

        /// <summary>
        /// 杂交结果
        /// </summary>
        /// <param name="seed">种子名称</param>
        /// <param name="name">玩家名称</param>
        /// <param name="result">种植结果</param>
        public HybridResult(string name, string seed, string result)
        {
            this.seed = seed;
            this.name = name;
            this.result = result;
        }
    }

    struct KDocsContext<T>
    {
        public struct ContextInner
        {
            public T argv;
        }
        public ContextInner Context;

        public KDocsContext(T data) 
        {
            Context = new ContextInner { argv = data };
        }
    }

    struct KDocsResponse
    {
        public string status;
        public string error;
    }

    class JsonContent : HttpContent
    {
        byte[] Data { get; }

        public string Text { get; }

        public JsonContent(object obj) 
        {
            Text = JsonConvert.SerializeObject(obj);
            Data = Encoding.UTF8.GetBytes(Text);

            Headers.Add("Content-Type", "application/json");
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return stream.WriteAsync(Data, 0, Data.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = Data.Length;
            return true;
        }
    }
}
