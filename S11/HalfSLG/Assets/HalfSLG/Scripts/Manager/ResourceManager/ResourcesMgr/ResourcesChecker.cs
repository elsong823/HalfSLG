//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System.IO;
//using WooEngine;
//using System;
//using UnityEngine.Networking;

//namespace ELTools
//{
//    public enum ResCheckRst
//    {
//        SUCCESS,
//        FAILED,
//        LOW_VERSION,    //版本等级太低，不能启动
//    }
//    public enum ResCheckType
//    {
//        VERIFY,         //文件校验中
//        CHECKUPDATE,    //检测文件是否需要更新
//        DOWNLOAD,       //文件下载中
//    }

//    public class ResFileInfo
//    {
//        public string fileName;
//        public string md5;
//        public int size;
//        public bool necessary = false;  //这个文件是否必须被下载到
//        public bool complete = false;   //这个文件是否下载完成
//        public int retryTimes = 0;      //重试次数

//        public string Desc
//        {
//            get
//            {
//                return string.Format("{0}|{1}|{2}", fileName, md5, size);
//            }
//        }
//    }


//    public class ResourcesChecker
//        : MonoBehaviour
//    {
//        private static ResourcesChecker instance = null;
//        public static bool agreeDownLoad = false;
//        private static bool checkUpdate
//        {
//            get { return WooEngine.AppConst.UpdateMode; }
//        }
//        private System.Action<ResCheckRst> m_checkCallback = null;
//        private System.Action<ResCheckType, string, int, int> m_progressCallback = null;
//        private System.Action<long, string> m_needUpdateCallback = null;
//        private int lastUploadProportion = -1;
//        public bool checkNetState = false;

//        public delegate void GameStartCallBack();
//        private GameObject announceObj = null;

//        private WaitForSeconds perWaitSecond = new WaitForSeconds(0.1f);
//        private WaitForSeconds oneSecond = new WaitForSeconds(1f);
//        private WaitForSeconds halfSecond = new WaitForSeconds(0.5f);

//#if GUONEI
//        public static CNFirstPage showPage;
//#else
//        public static NewFirstPage showPage;
//#endif

//        //创建实例
//        public static ResourcesChecker CreateInstance(GameObject mainObj)
//        {
//            if (instance != null)
//            {
//                EUtilityHelperL.LogWarning("资源检查器重复，忽视");
//                return instance;
//            }
//            if (mainObj == null)
//            {
//                EUtilityHelperL.LogError("创建资源检查器失败！空对象");
//                return null;
//            }
//            instance = mainObj.GetComponent<ELTools.ResourcesChecker>();
//            if (!instance)
//                instance = mainObj.AddComponent<ELTools.ResourcesChecker>();

//            return instance;
//        }

//        public static ResourcesChecker Instance
//        {
//            get
//            {
//                if (instance == null)
//                    EUtilityHelperL.LogError("请先创建实例");

//                return instance;
//            }
//        }

//        //检查资源情况
//        public void CheckResources(
//            System.Action<ResCheckRst> checkCallback,
//            System.Action<ResCheckType, string, int, int> progressCallback,
//            System.Action<long, string> needUpdateCallback
//            )
//        {
//            m_checkCallback = checkCallback;
//            m_progressCallback = progressCallback;
//            m_needUpdateCallback = needUpdateCallback;
//            StartCoroutine(Do());
//        }

//        private string GetPersistentPath(string fileName)
//        {
//            return WooEngine.Util.DataPath + fileName;
//        }
//        private string GetStreamingAssetsPath(string fileName)
//        {
//            return string.Format("{0}/{1}", Application.streamingAssetsPath, fileName);
//        }

//        private bool whereIsServerConfigReady = false;     //服务器存储配置文件的url就绪
//        private bool hotupdateFileReady = false;    //热更文件列表准备就绪


//        //ServerConfig下设置的最低启动等级
//        string serverConfigVersion = string.Empty;
//        int intServerConfigVersion = 0;

//        //当前Persistent版本
//        string localVersion = string.Empty;
//        int intLocalVersion = 0;

//        //当前StreamingAssets版本
//        string streamingVersion = string.Empty;
//        int intStreamingVersion = 0;

//        //当前热更服务器上的版本
//        string hotupdateVersion = string.Empty;
//        int intHotUpdateVersion = 0;

//        //更新说明
//        string upgradeDetials = string.Empty;

//        bool hotupdateFileWarning = false;
//        int timeout = 10;

//        long downloadTimeStamp = -1;

//        private bool CheckRequestSuccess(UnityWebRequest req)
//        {
//            if (req == null)
//                return false;

//            //没有错误
//            return string.IsNullOrEmpty(req.error)
//                && req.responseCode != 404
//                && req.responseCode != 500;
//        }

//        private IEnumerator CheckConnection(string title, string detail, string btn)
//        {
//            float startTimeStamp = Time.time;

//            //先做连接状态判断
//            while (!WooEngine.Util.NetAvailable)
//            {
//                checkNetState = false;
//                showPage.SetUpgradeTips(
//                title,
//                detail,
//                delegate ()
//                {
//                    checkNetState = true;
//                    showPage.HideUpgradeTips();
//                },
//                null,
//                btn,
//                string.Empty,
//                false);
//                yield return new WaitUntil(() => checkNetState);
//                //等一会儿再试试
//                yield return halfSecond;
//            }

//            yield break;
//        }

//        private bool waitForPlayerChoose = false;
//        private IEnumerator WaitForPlayerChoose(string title, string detail, string btn)
//        {
//            //提示玩家并等待
//            waitForPlayerChoose = false;

//            showPage.SetUpgradeTips(
//                 title,
//                 detail,
//                 delegate ()
//                 {
//                     waitForPlayerChoose = true;
//                     //继续等待
//                     showPage.HideUpgradeTips();
//                 },
//                 null,
//                 btn,
//                 string.Empty, false);

//            //等待玩家作出回复
//            yield return new WaitUntil(() => waitForPlayerChoose);
//        }

//        //从服务器上获取ServerConfig存储的路径(必须获取到，否则将无法游戏)
//        private IEnumerator RequestServerConfigURL()
//        {
//            //请求配置Post
//            WWWForm form = new WWWForm();
//            form.AddField("gameid", "1031");
//            form.AddField("channel", AppConst.channelID);
//            form.AddField("type", "1");
//            form.AddField("deviceId", SystemInfo.deviceUniqueIdentifier);
//            form.AddField("version", ELTools.EUtilityHelperL.GetVersion(AppConst.GameBaseVersion));             //添加一个版本号

//            Debug.LogWarning(string.Format("Request server config url->id:{0},channel:{1},type:{2},did:{3},ver:{4}",
//                1031,
//                AppConst.channelID,
//                1,
//                SystemInfo.deviceUniqueIdentifier,
//                ELTools.EUtilityHelperL.GetVersion(AppConst.GameBaseVersion)));

//            bool serverConfigOK = false;
//            //从配置服务器上下载WhereIsServerConfig
//            while (!serverConfigOK)
//            {
//                CheckerDebug("RequestServerConfigURL...");
//                using (UnityWebRequest www = UnityWebRequest.Post(PackageConfig.UrlForWhereIsServerConfig, form))
//                {
//                    www.chunkedTransfer = true;
//                    www.timeout = timeout;
//                    Debug.LogWarning(string.Format("正在从{0}请求WhereIsServerConfig", PackageConfig.UrlForWhereIsServerConfig));

//                    //提示正在连接服务器
//                    showPage.UpdatePromptText("10002", true);

//                    //从web上请求ServerConfig地址
//                    yield return www.Send();

//                    //判断请求结果
//                    if (!www.isError)
//                    {
//                        Debug.LogWarning(www.downloadHandler.text);

//                        //解析
//                        try
//                        {
//                            LitJson.JsonData jd = LitJson.JsonMapper.ToObject(www.downloadHandler.text);

//                            CheckerDebug("Config请求结果:" + www.downloadHandler.text);

//                            int rstCode = (int)jd["code"];
//                            if (rstCode == 1000)
//                            {
//                                LitJson.JsonData content = jd["content"];
//                                if (content != null)
//                                {
//                                    ELTools.PackageConfig.ServerConfigURL = ELTools.PackageConfig.FormatServerConfigURL(content["value"].ToString());
//                                    //是否白名单
//                                    if (content["whiteuser"] == null)
//                                        AppConst.InWhiteList = false;
//                                    else
//                                        AppConst.InWhiteList = (bool)content["whiteuser"];

//                                    PackageConfig.backupNormalServerConfigURL = string.Empty;

//                                    ////是否有备用配置服务器
//                                    //if (content["backupValue"] == null)
//                                    //    PackageConfig.backupNormalServerConfigURL = string.Empty;
//                                    //else
//                                    //    PackageConfig.backupNormalServerConfigURL = ELTools.PackageConfig.FormatServerConfigURL(content["backupValue"].ToString());

//                                    CheckerDebug(AppConst.InWhiteList ? "在白名单" : "不在白名单");
//                                    CheckerDebug("备用ServerConfig：" + PackageConfig.backupNormalServerConfigURL);

//                                    //文件下载成功
//                                    WooEngine.LuaHelper.SendTimeInfoByHttp(10001);
//                                    serverConfigOK = true;
//                                }
//                            }
//                            else
//                            {
//                                Debug.LogError("获取serverconfig的url时出错 code=? -> " + www.downloadHandler.text);
//                                WooEngine.LuaHelper.SendTimeInfoByHttp(10101);
//                            }
//                        }
//                        catch (Exception e)
//                        {
//                            Debug.LogError("解析serverconfig的url时出错 -> " + e.Message);
//                            WooEngine.LuaHelper.SendTimeInfoByHttp(10201);
//                        }
//                    }
//                    else
//                    {
//                        Debug.LogError("请求serverconfig的url时出错 -> " + www.error);
//                        WooEngine.LuaHelper.SendTimeInfoByHttp(10201);
//                    }

//                    if (!serverConfigOK)
//                        yield return WaitForPlayerChoose("10000", "10032", "10016");
//                }
//            }
//        }

//        //从服务器上获取配置文件(必须获取到，否则将无法游戏)
//        private IEnumerator RequestServerConfig()
//        {
//            serverConfigVersion = "0.0.0.0";
//            string serverConfigContent = string.Empty;

//            while (true)
//            {
//                CheckerDebug("RequestServerConfig...");

//                CheckerDebug(string.Format("正在从{0}请求服务器配置", PackageConfig.ServerConfigURL));

//                //提示正在连接服务器
//                showPage.UpdatePromptText("10002", true);

//                using (UnityWebRequest request = UnityWebRequest.Get(PackageConfig.ServerConfigURL))
//                {
//                    //从配置服务器上下载当前最低启动版本情况
//                    request.timeout = timeout;
//                    yield return request.Send();

//                    //没有成功
//                    if (!CheckRequestSuccess(request))
//                    {
//                        if (!string.IsNullOrEmpty(PackageConfig.backupNormalServerConfigURL))
//                        {
//                            using (UnityWebRequest backupReq = UnityWebRequest.Get(PackageConfig.backupNormalServerConfigURL))
//                            {
//                                CheckerDebug("准备向备用地址请求ServerConfig:" + PackageConfig.backupNormalServerConfigURL);
//                                backupReq.timeout = timeout;
//                                yield return backupReq.Send();

//                                if (!CheckRequestSuccess(backupReq))
//                                {
//                                    CheckerDebug("备用服务器请求ServerConfig->失败！");
//                                    showPage.UpdatePromptText("10003", true);
//                                    WooEngine.LuaHelper.SendTimeInfoByHttp(11001);
//                                    yield return halfSecond;
//                                    yield return WaitForPlayerChoose("10000", "10032", "10016");
//                                }
//                                else
//                                {
//                                    serverConfigContent = backupReq.downloadHandler.text;
//                                    break;
//                                }
//                            }
//                        }
//                        else
//                        {
//                            showPage.UpdatePromptText("10003", true);
//                            WooEngine.LuaHelper.SendTimeInfoByHttp(11001);
//                            yield return halfSecond;
//                            yield return WaitForPlayerChoose("10000", "10032", "10016");
//                        }
//                    }
//                    else
//                    {
//                        serverConfigContent = request.downloadHandler.text;
//                        break;
//                    }
//                }
//            }

//            //到此表示必请求到了正确的配置

//            //请求到了正确的配置~
//            WooEngine.LuaHelper.SendTimeInfoByHttp(10002);
//            //刷新启动配置开关
//            WooEngine.AppConst.ServerConfigStr = serverConfigContent.Trim('\r', '\n');
//            WooEngine.AppConst.ResetServerConfig();
//            CheckerDebug("请求到了服务器配置:\n" + WooEngine.AppConst.DisplayServerConfig());
//#if !UNITY_EDITOR
//            //设置热更开关！
//            WooEngine.AppConst.UpdateMode = (AppConst.GetServerConfig("HotUpdate").Equals("1"));
//            if (WooEngine.AppConst.UpdateMode)
//            {
//                Debug.Log("开启了热更！");
//            }
//            else
//                Debug.Log("关闭了热更");
//#else
//            if (Main.Instance.simulateHotupdate)
//            {
//                WooEngine.AppConst.UpdateMode = true;
//            }
//#endif

//            //获取最低启动版本
//            serverConfigVersion = AppConst.GetServerConfig("LowestVersion");
//            //重置热更服务器地址
//            AppConst.UpdateUrl = WooEngine.AppConst.GetServerConfig("HotUpdateUrl");
//            //保存备用下载地址
//            if (WooEngine.AppConst.GetServerConfig("BackupUpdateUrl").Length > 5)
//                AppConst.BackupUpdateUrl = WooEngine.AppConst.GetServerConfig("BackupUpdateUrl");

//            if (!string.IsNullOrEmpty(AppConst.BackupUpdateUrl))
//                CheckerDebug("设置了备用热更地址:" + AppConst.BackupUpdateUrl);
//            else
//                CheckerDebug("没有设置备用热更地址");
//        }

//        //从热更服务器上获取文件列表(必须获取到，否则无法游戏)
//        private IEnumerator RequestHotUpdateFileList(List<ResFileInfo> tempFileList)
//        {
//            //打点，开始请求服务器配置文件
//            WooEngine.LuaHelper.SendTimeInfoByHttp(60000);
//            tempFileList.Clear();
//            hotupdateVersion = string.Empty;
//            //需要请求的热更文件列表
//            string fileListContent = string.Empty;
//            //必须要请求到这个文件

//            while (true)
//            {
//                CheckerDebug(string.Format("正在从{0}请求热更文件", PackageConfig.HotUpdateFileList));

//                //提示正在连接服务器
//                showPage.UpdatePromptText("10005", true);

//                using (UnityWebRequest request = UnityWebRequest.Get(PackageConfig.HotUpdateFileList))
//                {
//                    request.timeout = timeout;
//                    //从配置服务器上下载当前最低启动版本情况
//                    yield return request.Send();

//                    //我曹，文件居然请求失败！
//                    if (!CheckRequestSuccess(request))
//                    {
//                        //好险，幸亏有备用地址
//                        if (!string.IsNullOrEmpty(PackageConfig.BackupHotUpdateFileList))
//                        {
//                            using (UnityWebRequest backupReq = UnityWebRequest.Get(PackageConfig.BackupHotUpdateFileList))
//                            {
//                                CheckerDebug(string.Format("正在从备用地址 {0} 请求热更文件", PackageConfig.BackupHotUpdateFileList));
//                                backupReq.timeout = timeout;
//                                yield return backupReq.Send();
//                                if (!CheckRequestSuccess(backupReq))
//                                {
//                                    //我曹，连备用的都失效了！
//                                    Debug.LogError(string.Format("ResChecker:请求服务器热更文件列表失败！:{0}->{1}", PackageConfig.BackupHotUpdateFileList, string.IsNullOrEmpty(request.error) ? "404" : request.error));
//                                    showPage.UpdatePromptText("10027", true);
//                                    WooEngine.LuaHelper.SendTimeInfoByHttp(61001);
//                                    yield return halfSecond;
//                                    yield return WaitForPlayerChoose("10000", "10032", "10016");
//                                }
//                                else
//                                {
//                                    fileListContent = backupReq.downloadHandler.text;
//                                    break;
//                                }
//                            }
//                        }
//                        else
//                        {
//                            Debug.LogError(string.Format("ResChecker:请求服务器热更文件列表失败！:{0}->{1}", PackageConfig.HotUpdateFileList, string.IsNullOrEmpty(request.error) ? "404" : request.error));
//                            showPage.UpdatePromptText("10027", true);
//                            WooEngine.LuaHelper.SendTimeInfoByHttp(61001);
//                            yield return halfSecond;
//                            yield return WaitForPlayerChoose("10000", "10032", "10016");
//                        }
//                    }
//                    else
//                    {
//                        //请求成功
//                        fileListContent = request.downloadHandler.text;
//                        break;
//                    }
//                }
//            }

//            WooEngine.LuaHelper.SendTimeInfoByHttp(60001);
//            CheckerDebug("热更文件请求成功！");
//            string[] args = fileListContent.Split('\n');
//            for (int i = 0; i < args.Length; ++i)
//            {
//                if (i == 0)
//                {
//                    //获取版本等级
//                    hotupdateVersion = args[0].Replace(PackageConfig.Version, string.Empty);
//                    CheckerDebug("热更文件的版本为" + hotupdateVersion);
//                }
//                else if (args[i].Length < 1)
//                {
//                    continue;
//                }
//                else
//                {
//                    string[] temps = args[i].Split('|');
//                    if (temps.Length == 3)
//                    {
//                        string fileName = temps[0];
//                        string md5 = temps[1];
//                        int size = int.Parse(temps[2]);
//                        ResFileInfo info = new ResFileInfo();
//                        info.fileName = fileName;
//                        info.md5 = md5;
//                        info.size = size;
//                        tempFileList.Add(info);
//                    }
//                }
//            }
//        }

//        //更新下载进度
//        private void UpdateDownloadProgress(float totalSize, float already, float currentFile, float currentProgress, long startDownloadTick)
//        {
//            if (downloadTimeStamp < 0)
//            {
//                downloadTimeStamp = DateTime.Now.Ticks;
//                return;
//            }
//            //时间消耗
//            long timeCost = DateTime.Now.Ticks - downloadTimeStamp;
//            //推算已下载量
//            float resDownloaded = already + currentFile * currentProgress;
//            //下载进度
//            float rate = resDownloaded / totalSize;
//            float downloadSpeed = resDownloaded / (int)(timeCost * 0.0000001f);
//            showPage.UpdateProgress(rate);
//            //更新下载速度
//            int timeElpasedSec = (int)(timeCost * 0.0000001f);
//            //int timeElpasedSec = (int)((DateTime.Now.Ticks - startDownloadTick) * 0.0000001f);
//            timeElpasedSec = Mathf.Max(timeElpasedSec, 1);
//            float downLoadSpeed = resDownloaded / timeElpasedSec;
//            //是否隐藏下载速度
//            string hideDownloadSpeed = AppConst.GetServerConfig("HideDownloadSpeed");
//            string strDownLoadSpeed = string.Format("{0}/s", Util.CountSizeString((long)downLoadSpeed));
//            if (hideDownloadSpeed.ToLower().Equals("yes"))
//                strDownLoadSpeed = string.Empty;

//            //更新下载进度
//            showPage.UpdateDownLoadSpeed(string.Format("{0} {1}/{2}", strDownLoadSpeed, Util.CountSizeString((long)(already + currentFile * currentProgress)), Util.CountSizeString((long)totalSize)));
//        }

//        private bool CheckHotUpdateReady(List<ResFileInfo> needCopyList)
//        {
//            hotupdateFileWarning = false;
//            for (int i = 0; i < needCopyList.Count; ++i)
//            {
//                //文件下载完毕
//                if (needCopyList[i].complete)
//                    continue;

//                //超过了最大尝试次数，但是没有下载完毕，算了，但是要发一个警告
//                if (!needCopyList[i].necessary && needCopyList[i].retryTimes > 3)
//                {
//                    hotupdateFileWarning = true;
//                    continue;
//                }

//                return false;
//            }
//            return true;
//        }

//        //从热更服务器上获取文件（如果本地不包含这个文件，则必须获取到，否则无法游戏；如果本地有，但是没有更新到，可以继续游戏）
//        private IEnumerator RequestHotUpdateFiles(List<ResFileInfo> needCopyList)
//        {
//            WooEngine.LuaHelper.SendTimeInfoByHttp(60005);

//            CheckerDebug("排序中...");
//            //排序
//            needCopyList.Sort(delegate (ResFileInfo r1, ResFileInfo r2)
//            {
//                return r2.size - r1.size;
//            });
//            CheckerDebug("排序结束，开始下载...");

//            //合计下载大小
//            float alreadyDownloadSize = 0f;

//            //计算全部下载大小
//            float totalSize = 0.1f;
//            for (int i = 0; i < needCopyList.Count; ++i)
//                totalSize += needCopyList[i].size;

//            long startDownLoadTick = DateTime.Now.Ticks;
//            while (!CheckHotUpdateReady(needCopyList))
//            {
//                //开始下载文件
//                for (int i = 0; i < needCopyList.Count; ++i)
//                {
//                    //这个文件已经下载完毕
//                    if (needCopyList[i].complete)
//                        continue;

//                    string fromUrl = string.Format("{0}{1}?{2}", PackageConfig.HotUpdateUrl, needCopyList[i].fileName, System.DateTime.Now.Ticks);
//                    CheckerDebug("正在从主服务器请求热更文件:" + fromUrl);
//                    string toLocal = GetPersistentPath(needCopyList[i].fileName);
//                    byte[] fileData = null;
//                    using (UnityWebRequest request = UnityWebRequest.Get(fromUrl))
//                    {
//                        request.Send();
//                        while (!request.isDone)
//                        {
//                            //实时更新
//                            UpdateDownloadProgress(totalSize, alreadyDownloadSize, needCopyList[i].size, Mathf.Clamp01(request.downloadProgress), startDownLoadTick);
//                            yield return perWaitSecond;
//                        }
//                        //文件没有下载成功，尝试去备份服务器下载
//                        if (!CheckRequestSuccess(request))
//                        {
//                            //保存了备份服务器
//                            if (!string.IsNullOrEmpty(PackageConfig.BackupHotUpdateUrl))
//                            {
//                                fromUrl = string.Format("{0}{1}?{2}", PackageConfig.BackupHotUpdateUrl, needCopyList[i].fileName, System.DateTime.Now.Ticks);
//                                CheckerDebug("正在从备份服务器请求热更文件:" + fromUrl);
//                                using (UnityWebRequest backupReq = UnityWebRequest.Get(fromUrl))
//                                {
//                                    backupReq.Send();
//                                    while (!backupReq.isDone)
//                                    {
//                                        //实时更新
//                                        UpdateDownloadProgress(totalSize, alreadyDownloadSize, needCopyList[i].size, Mathf.Clamp01(backupReq.downloadProgress), startDownLoadTick);
//                                        yield return perWaitSecond;
//                                    }
//                                    //没有成功
//                                    if (!CheckRequestSuccess(backupReq))
//                                    {
//                                        //尴尬了
//                                        WooEngine.LuaHelper.SendTimeInfoByHttp(61006);
//                                        Debug.LogError(string.Format("请求文件{0}失败！", fromUrl));

//                                        //打点记录
//                                        LuaHelper.RecordDownLoadFailed(PlayerPrefs.GetString(PackageConfig.LocalVersionKey, "0.0.0.0"),
//                                            fromUrl,
//                                            needCopyList[i].retryTimes.ToString());

//                                        //增加重试次数
//                                        needCopyList[i].retryTimes++;
//                                    }
//                                    else
//                                    {
//                                        fileData = backupReq.downloadHandler.data;
//                                    }
//                                }
//                            }
//                            else
//                            {
//                                //文件下载失败
//                                WooEngine.LuaHelper.SendTimeInfoByHttp(61006);
//                                Debug.LogError(string.Format("请求文件{0}失败！", fromUrl));

//                                //打点记录
//                                LuaHelper.RecordDownLoadFailed(PlayerPrefs.GetString(PackageConfig.LocalVersionKey, "0.0.0.0"),
//                                    fromUrl,
//                                    needCopyList[i].retryTimes.ToString());

//                                //增加重试次数
//                                needCopyList[i].retryTimes++;
//                            }
//                        }
//                        else
//                        {
//                            fileData = request.downloadHandler.data;
//                        }
//                    }

//                    //文件下载成功
//                    if (fileData != null)
//                    {
//                        //本地文件存在
//                        if (File.Exists(toLocal))
//                            File.Delete(toLocal);

//                        if (!Directory.Exists(Path.GetDirectoryName(toLocal)))
//                            Directory.CreateDirectory(Path.GetDirectoryName(toLocal));

//                        File.WriteAllBytes(toLocal, fileData);
//                        string fileMD5 = EUtilityHelperL.CalcMD5(toLocal);

//                        //标记文件已经下载完毕
//                        needCopyList[i].complete = true;

//                        if (needCopyList[i].md5.Equals(fileMD5))
//                        {
//                            //Debug.Log(string.Format("文件下载成功，md5相同为：{0}", fileMD5));
//                        }
//                        else
//                        {
//                            //做一个特殊标记
//                            LuaHelper.RecordDownLoadFailed(PlayerPrefs.GetString(PackageConfig.LocalVersionKey, "0.0.0.0"),
//                                fromUrl,
//                                "-1");
//                            Debug.LogError(string.Format("文件下载成功，md5不相同为：{0}->{1}", fileMD5, needCopyList[i].md5));
//                        }

//                        //更新下载进度
//                        UpdateDownloadProgress(totalSize, alreadyDownloadSize, needCopyList[i].size, 1f, startDownLoadTick);

//                        //已经下载量
//                        alreadyDownloadSize += needCopyList[i].size;

//                        //每5%上传一次
//                        int proportion = Mathf.CeilToInt((i + 1) * 1f / needCopyList.Count * 100f);
//                        if (proportion % 5 == 0)
//                        {
//                            proportion = Mathf.Clamp(proportion, 0, 99);
//                            if (proportion != lastUploadProportion)
//                            {
//                                //通知进度
//                                WooEngine.LuaHelper.SendTimeInfoByHttp(int.Parse(string.Format("60{0:00}6", proportion)));
//                                lastUploadProportion = proportion;
//                            }
//                        }
//                    }
//                    fileData = null;
//                }

//                showPage.UpdateDownLoadSpeed(string.Empty);
//            }
//        }

//        //获取更新说明
//        private IEnumerator RequestUpgradeDetail()
//        {
//            WooEngine.LuaHelper.SendTimeInfoByHttp(60013);

//            CheckerDebug("正在请求更新说明");

//            //请求到的内容
//            upgradeDetials = string.Empty;

//            CheckerDebug(string.Format("正在从{0}请求更新说明", PackageConfig.UpgradeDetailURL));

//            using (UnityWebRequest request = UnityWebRequest.Get(PackageConfig.UpgradeDetailURL))
//            {
//                request.timeout = timeout;
//                yield return request.Send();

//                //没有成功
//                if (!CheckRequestSuccess(request))
//                {
//                    if (!string.IsNullOrEmpty(PackageConfig.BackupUpgradeDetailURL))
//                    {
//                        CheckerDebug("准备向备用地址请求更新说明:" + PackageConfig.BackupUpgradeDetailURL);
//                        //请求备用
//                        using (UnityWebRequest backupReq = UnityWebRequest.Get(PackageConfig.BackupUpgradeDetailURL))
//                        {
//                            backupReq.timeout = timeout;

//                            yield return backupReq.Send();

//                            if (!CheckRequestSuccess(backupReq))
//                            {
//                                CheckerDebug("备用服务器请求更新说明->失败！");
//                                showPage.UpdatePromptText("10006", true);
//                                WooEngine.LuaHelper.SendTimeInfoByHttp(61004);
//                                yield return halfSecond;
//                            }
//                            else
//                            {
//                                upgradeDetials = backupReq.downloadHandler.text;
//                                WooEngine.LuaHelper.SendTimeInfoByHttp(60004);
//                            }
//                        }
//                    }
//                    else
//                    {
//                        showPage.UpdatePromptText("10006", true);
//                        WooEngine.LuaHelper.SendTimeInfoByHttp(61004);
//                        yield return halfSecond;
//                    }
//                }
//                else
//                {
//                    upgradeDetials = request.downloadHandler.text;
//                    WooEngine.LuaHelper.SendTimeInfoByHttp(60004);
//                }
//            }
//        }

//        private IEnumerator Do()
//        {
//            CheckerDebug("CHECKER:启动游戏，准备检查游戏的更新状态");

//            showPage.ActiveProgress(true);
//            showPage.ActiveProgressText(false);
//            SetLoadingActive(false);

//            //做一次网络连接检测
//            CheckerDebug("CHECKER:做一次网络连接检测");
//            yield return CheckConnection("10000", "10015", "10016");

//            CheckerDebug("CHECKER:准备向web请求ServerConfig地址");

//            //提示正在启动游戏
//            showPage.UpdatePromptText("10001", true);


//#if !HEHEDA || HAPPY_PACKAGE


//#if !HAPPY_PACKAGE
//            //正式包需要连web 获取ServerConfig的地址！
//            WooEngine.LuaHelper.SendTimeInfoByHttp(10000);
//            yield return RequestServerConfigURL();
//#endif

//            //获取服务器config，必须要获取到呢~
//            yield return RequestServerConfig();

//            //当前配置服务器上所配置的版本等级
//            intServerConfigVersion = ELTools.EUtilityHelperL.GetVersion(serverConfigVersion);
//            CheckerDebug(string.Format("CHECKER:当前配置服务器上的 最低 版本为 : {0}", serverConfigVersion));

//            //尝试获取本地版本等级
//            localVersion = PlayerPrefs.GetString(PackageConfig.LocalVersionKey, "0.0.0.0");
//            CheckerDebug(string.Format("CHECKER:本地版本为 : {0}", localVersion));

//            intLocalVersion = ELTools.EUtilityHelperL.GetVersion(localVersion);
//            if (intLocalVersion == 0)
//            {
//                Debug.LogWarning("CHECKER:当前为首次启动游戏，需要等待的久一点。");
//                //提示首次启动游戏要等待的久一点
//                WooEngine.LuaHelper.SendTimeInfoByHttp(20001);
//                showPage.UpdatePromptText("10020", true);
//            }

//            //提示要开始获取本地版本等级及文件列表
//            showPage.UpdatePromptText("10021", true);
//#endif

//            //获取streamingAssets中记录的等级
//            string pathInStreaming = GetStreamingAssetsPath(PackageConfig.StreamingVersionFile);
//            streamingVersion = string.Empty;
//            List<ResFileInfo> tempFileList = new List<ResFileInfo>();

//#if UNITY_EDITOR || UNITY_IOS
//            string[] args = File.ReadAllLines(pathInStreaming);
//            if (args != null && args.Length > 0)
//            {
//                WooEngine.LuaHelper.SendTimeInfoByHttp(30001);
//                for (int i = 0; i < args.Length; ++i)
//                {
//                    if (i == 0)
//                    {
//                        //获取版本等级
//                        streamingVersion = args[0].Replace(PackageConfig.Version, string.Empty);
//                    }
//                    else if (args[i].Length < 1)
//                    {
//                        continue;
//                    }
//                    else
//                    {
//                        string[] temps = args[i].Split('|');
//                        if (temps.Length == 3)
//                        {
//                            string fileName = temps[0];
//                            string md5 = temps[1];
//                            int size = int.Parse(temps[2]);
//                            ResFileInfo info = new ResFileInfo();
//                            info.fileName = fileName;
//                            info.md5 = md5;
//                            info.size = size;
//                            tempFileList.Add(info);
//                        }
//                    }
//                }
//            }
//            else
//            {
//                //提示本地版本等级及文件列表获取失败
//                WooEngine.LuaHelper.SendTimeInfoByHttp(32002);
//                showPage.UpdatePromptText("10022", false);
//                EUtilityHelperL.LogError("没有办法获取StreamingFile,无法获取本地版本等级！！");
//            }
//#else
//            using (WWW www = new WWW(pathInStreaming))
//            {
//                yield return www;
//                if (!string.IsNullOrEmpty(www.error))
//                {
//                    //文件读取失败
//                    //提示本地版本等级及文件列表获取失败
//                    WooEngine.LuaHelper.SendTimeInfoByHttp(32002);
//                    showPage.UpdatePromptText("10022", false);
//                    EUtilityHelperL.LogError("没有办法获取StreamingFile,无法获取本地版本等级！！");
//                }
//                else
//                {
//                    WooEngine.LuaHelper.SendTimeInfoByHttp(30001);
//                    //找到文件
//                    string temp = www.text;
//                    string[] streamingFileListContent = temp.Split('\n');
//                    for (int i = 0; i < streamingFileListContent.Length; ++i)
//                    {
//                        if (i == 0)
//                        {
//                            //获取版本等级
//                            streamingVersion = streamingFileListContent[0].Replace(PackageConfig.Version, string.Empty);
//                        }
//                        else if (streamingFileListContent[i].Length < 1)
//                        {
//                            continue;
//                        }
//                        else
//                        {
//                            string[] temps = streamingFileListContent[i].Split('|');
//                            if (temps.Length == 3)
//                            {
//                                string fileName = temps[0];
//                                string md5 = temps[1];
//                                int size = int.Parse(temps[2]);
//                                ResFileInfo info = new ResFileInfo();
//                                info.fileName = fileName;
//                                info.md5 = md5;
//                                info.size = size;
//                                tempFileList.Add(info);
//                            }
//                        }
//                    }
//                }
//            }
//#endif

//            //获取本地版本
//            intStreamingVersion = ELTools.EUtilityHelperL.GetVersion(streamingVersion);
//            CheckerDebug(string.Format("StreamingVersion Version : {0}", streamingVersion));

//            //判断是否需要强更
//            if (intServerConfigVersion > Mathf.Max(intLocalVersion, intStreamingVersion))
//            {
//                showPage.UpdatePromptText("10023", false);
//                WooEngine.LuaHelper.SendTimeInfoByHttp(40000);
//                //需要强更，不用再挣扎了！
//                if (m_checkCallback != null)
//                {
//                    CheckerDebug("本地版本过低，需要强更！");
//                    m_checkCallback(ResCheckRst.LOW_VERSION);
//                }
//                yield break;
//            }

//            //这里特别注意！
//            //在日本iOS里，苹果审核不允许解压缩文件或者下载文件的读条，因此专为iOS设置了：
//            //从streamingAssets中直接读取文件！
//            //需要serverConfig上的配置发生改动后生效
//            //这个傻逼规定已经不用了！！草
//#if UNITY_IOS
//            //从Streaming assets文件夹中读取文件
//            Debug.Log("当前服务器配置的启动项:" + AppConst.GetServerConfig("LoadFromStreamingAssets"));

//            if (AppConst.GetServerConfig("LoadFromStreamingAssets").Equals("true"))
//            {
//                AppConst.LoadResFromStreamingAssets = true;

//                Debug.LogWarning("Load res from streaming assets.");

//                if (PlayerPrefs.GetInt("isFirstCheck", 0) == 0)
//                    PlayerPrefs.SetInt("isFirstCheck", 1);
                
//                showPage.UpdatePromptText("10031", false);

//                showPage.ActiveProgress(false);

//                PlayerPrefs.SetInt(AppConst.REPLACED_FIRSTPAGE, 1);

//                //yield return new WaitForSeconds(stepHold);

//                WooEngine.LuaHelper.SendTimeInfoByHttp(80000);
//                m_checkCallback(ResCheckRst.SUCCESS);
//                yield break;
//            }
//#endif

//            //需要从streaming 拷贝到 persistent文件夹下的文件列表
//            List<ResFileInfo> needCopyList = new List<ResFileInfo>();
//            //当前本地文件的文件列表
//            Dictionary<string, ResFileInfo> persistentFilesDic = new Dictionary<string, ResFileInfo>();
//            //判断当前是否需要从streamingAssets进行拷贝呢？
//            showPage.UpdatePromptText("10018", true);
//            int streamingAssetsCopied = PlayerPrefs.GetInt("STREAMING_COPIED" + streamingVersion, 0);
//            CheckerDebug("当前Streaming拷贝情况:" + streamingAssetsCopied);
//            if (intLocalVersion <= intStreamingVersion && streamingAssetsCopied != 1)
//            {
//                CheckerDebug("做StreamingAssets文件检测，判断是否需要文件拷贝");
//                WooEngine.LuaHelper.SendTimeInfoByHttp(50000);
//                //校验stremaing文件和per文件
//                for (int i = 0; i < tempFileList.Count; ++i)
//                {
//                    string persistentFilePath = GetPersistentPath(tempFileList[i].fileName);
//                    if (File.Exists(persistentFilePath))
//                    {
//                        //获取本地md5
//                        string persistentMd5 = EUtilityHelperL.CalcMD5(persistentFilePath);
//                        if (tempFileList[i].md5 != persistentMd5)
//                        {
//                            //IOS下需要判断下是否需要拷贝
//                            string overwriteAssets = AppConst.GetServerConfig("OverwriteAssets");
//                            if (overwriteAssets.Equals("Yes"))
//                            {
//                                needCopyList.Add(tempFileList[i]);
//                                persistentFilesDic[tempFileList[i].fileName] = tempFileList[i];
//                            }
//                            else
//                            {
//                                CheckerDebug(string.Format("存在文件不同，但是由于忽视逻辑，没有覆盖~ -> {0}", persistentFilePath));
//                            }
//                        }
//                    }
//                    else
//                    {
//                        needCopyList.Add(tempFileList[i]);
//                        persistentFilesDic[tempFileList[i].fileName] = tempFileList[i];
//                    }

//                }

//                //需要进行文件拷贝 dow
//                if (needCopyList.Count > 0)
//                {
//                    CheckerDebug("需要进行文件拷贝！Streaming -> Persistent.文件数量为：" + needCopyList.Count);
//                    showPage.UpdateProgress(0f);
//                    showPage.UpdatePromptText("10025", true);
//                    WooEngine.LuaHelper.SendTimeInfoByHttp(50001);
//                }
//                else
//                {
//                    WooEngine.LuaHelper.SendTimeInfoByHttp(50002);
//                }

//                //文件拷贝
//                for (int i = 0; i < needCopyList.Count; ++i)
//                {
//                    string from = GetStreamingAssetsPath(needCopyList[i].fileName);
//                    string to = GetPersistentPath(needCopyList[i].fileName);
//                    if (WooEngine.AppConst.GetServerConfig("ShowUpdateDebugInfo").ToLower() == "yes")
//                    {
//                        Debug.Log(string.Format("Copy file from {0} -> {1}", from, to));
//                    }
//#if UNITY_EDITOR || UNITY_IOS

//                    string toDir = Path.GetDirectoryName(to);
//                    if (!Directory.Exists(toDir))
//                        Directory.CreateDirectory(toDir);

//                    //文件拷贝
//                    File.Copy(from, to, true);
//                    //更新进度
//                    float _progress = (i + 1f) / needCopyList.Count;
//                    if (showPage == null)
//                    {
//                        Debug.LogError("没有FirstPage！");
//                        yield return null;
//                    }
//                    showPage.UpdateProgress(_progress);
//                    yield return null;
//#else
//                    using (WWW www = new WWW(from))
//                    {
//                        yield return www;
//                        if (string.IsNullOrEmpty(www.error))
//                        {
//                            if (File.Exists(to))
//                                File.Delete(to);

//                            string destDir = Path.GetDirectoryName(to);
//                            if (!Directory.Exists(destDir))
//                                Directory.CreateDirectory(destDir);

//                            File.WriteAllBytes(to, www.bytes);
//                        }
//                        //更新进度
//                        float copyProgress = (i + 1f) / needCopyList.Count;
//                        showPage.UpdateProgress(copyProgress);
//                    }
//#endif
//                }

//                //表示文件拷贝完成(无需判断是否需要)
//                WooEngine.LuaHelper.SendTimeInfoByHttp(50003);

//                //重置版本号
//                intLocalVersion = intStreamingVersion;
//                //记录拷贝情况
//                PlayerPrefs.SetInt("STREAMING_COPIED" + streamingVersion, 1);
//                //保存到本地
//                PlayerPrefs.SetString(PackageConfig.LocalVersionKey, streamingVersion);

//                showPage.UpdatePromptText("10024", true);
//                showPage.ActiveProgressText(false);
//            }
//            else
//            {
//                WooEngine.LuaHelper.SendTimeInfoByHttp(50003);
//                showPage.UpdatePromptText("10024", true);
//                showPage.ActiveProgressText(false);
//            }

//#if HEHEDA && !HAPPY_PACKAGE
//            WooEngine.AppConst.UpdateMode = false;
//#endif

//            //开始做热更新
//            if (checkUpdate)
//            {
//                //尝试获取服务器列表
//                yield return RequestHotUpdateFileList(tempFileList);

//                intHotUpdateVersion = ELTools.EUtilityHelperL.GetVersion(hotupdateVersion);
//                CheckerDebug(string.Format("当前服务器上的资源版本 : {0}", hotupdateVersion));

//                //需要做热更检测
//                //如果在白名单中，则会降级
//                if (AppConst.InWhiteList || intLocalVersion <= intHotUpdateVersion)
//                {
//                    WooEngine.LuaHelper.SendTimeInfoByHttp(60002);
//                    //准备做下载文件列表检测
//                    needCopyList.Clear();
//                    long totalSize = 0;
//                    //需要做文件校验
//                    for (int i = 0; i < tempFileList.Count; i++)
//                    {
//                        string _fileName = tempFileList[i].fileName;
//                        string persistentFilePath = GetPersistentPath(_fileName);
//                        if (File.Exists(persistentFilePath))
//                        {
//                            string persistentMd5 = "";
//                            if (persistentFilesDic.ContainsKey(_fileName))
//                                persistentMd5 = persistentFilesDic[_fileName].md5;
//                            else
//                            {
//                                //没有必须创造一个，否则后面的文件列表保存不上
//                                persistentMd5 = ELTools.EUtilityHelperL.CalcMD5(persistentFilePath);
//                                persistentFilesDic[_fileName] = new ResFileInfo();
//                                persistentFilesDic[_fileName].md5 = persistentMd5;
//                                persistentFilesDic[_fileName].size = (int)ELTools.EUtilityHelperL.GetFileSize(persistentFilePath);
//                                persistentFilesDic[_fileName].fileName = _fileName;
//                            }
//                            if (tempFileList[i].md5 != persistentMd5)
//                            {
//                                needCopyList.Add(tempFileList[i]);
//                                //这个文件存在，也可以不必须下载完毕
//                                tempFileList[i].necessary = false;
//                                tempFileList[i].complete = false;
//                                tempFileList[i].retryTimes = 0;
//                                totalSize += tempFileList[i].size;
//                                persistentFilesDic[_fileName] = tempFileList[i];
//                            }
//                        }
//                        else
//                        {
//                            //这个文件必须被下载到！
//                            needCopyList.Add(tempFileList[i]);
//                            tempFileList[i].necessary = true;
//                            tempFileList[i].complete = false;
//                            tempFileList[i].retryTimes = 0;
//                            totalSize += tempFileList[i].size;
//                            persistentFilesDic[tempFileList[i].fileName] = tempFileList[i];
//                        }
//                    }

//                    //打点标记
//                    if (needCopyList.Count > 0)
//                        WooEngine.LuaHelper.SendTimeInfoByHttp(60003);
//                    else
//                        WooEngine.LuaHelper.SendTimeInfoByHttp(63003);

//                    // 此处可做是否下载判定
//                    if (needCopyList.Count > 0 && totalSize > 0)
//                    {
//                        //判断是否需要给玩家提示
//                        bool showNotice = true;
//                        string hideNoticeInWifi = AppConst.GetServerConfig("HideNoticeInWifi");
//                        string hideNoticeKBLimit = AppConst.GetServerConfig("HideNoticeKBLimit");
//                        string ignoreUpgradeDetail = AppConst.GetServerConfig("IgnoreUpgradeDetail");

//                        if (!ignoreUpgradeDetail.ToLower().Equals("yes"))
//                        {
//                            //请求更新说明
//                            yield return RequestUpgradeDetail();
//                        }
//                        else
//                        {
//                            //忽视更新说明
//                            WooEngine.LuaHelper.SendTimeInfoByHttp(60004);
//                        }

//                        //如果玩家在wifi环境 且 允许隐藏
//                        if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork
//                            && hideNoticeInWifi.ToLower().Equals("yes"))
//                        {
//                            showNotice = false;
//                        }
//                        else
//                        {
//                            int limitSize = 0;
//                            //设置了不显示下载提示的mb数
//                            if (int.TryParse(hideNoticeKBLimit, out limitSize) && Mathf.FloorToInt(totalSize / 1024f) < limitSize)
//                            {
//                                CheckerDebug("设置了下载kb显示限制，因此隐藏了提示");
//                                showNotice = false;
//                            }
//                        }

//                        //需要提示玩家确认
//                        if (showNotice)
//                        {
//                            //到此表示开始更新
//                            //等待确认
//                            agreeDownLoad = false;
//                            var configMgr = AppFacade.Instance.GetManager<ConfigManager>(ManagerName.Config);
//                            string _format = configMgr.GetStartUpLangID("10041");
//                            string downloadStr = string.Format(_format, Util.CountSizeString(totalSize));
//                            CheckerDebug("UpgradeDetail->" + upgradeDetials);
//                            if (upgradeDetials.Length < 5)
//                            {
//                                //没有设置更新说明
//                                //只是建议在wifi环境下下载
//                                showPage.SetUpgradeTips("10014",
//                                    downloadStr,
//                                    delegate ()
//                                    {
//                                        agreeDownLoad = true;
//                                    },
//                                    null, "10012", string.Empty, false);
//                            }
//                            else
//                            {
//                                //显示确认框
//                                showPage.SetUpgradeTips("10014",
//                                    string.Format("{0}\n{1}", downloadStr, upgradeDetials),
//                                    delegate ()
//                                    {
//                                        agreeDownLoad = true;
//                                    },
//                                    null, "10012", string.Empty, false);
//                            }

//                            //等待用户同意下载
//                            yield return new WaitUntil(() => agreeDownLoad);
//                        }

//                        showPage.HideUpgradeTips();
//                        showPage.UpdateProgress(0f);
//                        showPage.ActiveProgressText(true);
//                        showPage.UpdatePromptText("10008", true);

//                        //开始热更新
//                        yield return RequestHotUpdateFiles(needCopyList);

//                        //结束热更新
//                        WooEngine.LuaHelper.SendTimeInfoByHttp(6);
//                        showPage.UpdatePromptText("10028", true);
//                        yield return new WaitForSeconds(1f);
//                        showPage.ActiveProgressText(false);
//                    }
//                    else
//                    {
//                        //不需要做热更
//                        Debug.Log("不需要做热更新！？！");
//                        Debug.Log("当前记录的文件列表：" + persistentFilesDic.Count);
//                        showPage.UpdatePromptText("10029", true);
//                        WooEngine.LuaHelper.SendTimeInfoByHttp(63002);
//                    }
//                    //保存本地版本
//                    PlayerPrefs.SetString(PackageConfig.LocalVersionKey, hotupdateVersion);
//                }
//                else
//                {
//                    //TODO
//                    //本地版本 > 热更服务器资源版本
//                    showPage.UpdatePromptText("10029", true);
//                    WooEngine.LuaHelper.SendTimeInfoByHttp(62002);
//                }
//            }
//            else
//            {
//                CheckerDebug("没有开启热更新！");
//                showPage.UpdatePromptText("10030", true);
//                WooEngine.LuaHelper.SendTimeInfoByHttp(63000);
//            }

//            //yield return new WaitForSeconds(stepHold);

//            //这里以前出过一个错，如果当前已经做过热更，本地版本已经>streamingasset版本
//            //但是又无法连接资源服务器，则会删掉本地文件列表，导致初始化失败无法进入游戏
//            //因此对本地文件数量做一个判断
//            if (persistentFilesDic != null && persistentFilesDic.Count > 0)
//            {
//                WooEngine.LuaHelper.SendTimeInfoByHttp(70000);
//                //保存本地文件
//                List<string> localFileList = new List<string>();
//                foreach (var fileInfo in persistentFilesDic)
//                {
//                    localFileList.Add(fileInfo.Value.Desc);
//                }
//                string localFileListFilePath = GetPersistentPath(WooEngine.AppConst.fileList);
//                if (File.Exists(localFileListFilePath))
//                    File.Delete(localFileListFilePath);

//                File.WriteAllLines(localFileListFilePath, localFileList.ToArray());
//            }
//            else
//                WooEngine.LuaHelper.SendTimeInfoByHttp(71000);

//            Debug.LogWarning("启动游戏，当前版本号为：" + PlayerPrefs.GetString(PackageConfig.LocalVersionKey));

//            //刷新一下版本号
//            if (showPage)
//            {
//                showPage.UpdateVersionShow();
//            }

//            //必须要尝试追加一下manifest文件，否则会出现白图的情况！
//            //因为firstpage已经读入过streamingAsset了
//            if (needCopyList.Count > 0)
//            {
//                if (ELTools.ResourceManager.Instance)
//                    ELTools.ResourceManager.Instance.SetupBundleRelationships(Path.Combine(Util.DataPath, "StreamingAssets"));
//            }
//            //判断能否直接进入游戏
//            CheckCanIntoGame();
//        }

//        IEnumerator CheckServerActive()
//        {
//            yield return RequestServerConfig();
//            if (AppConst.GetServerConfig("ServerState") == "0")
//                yield return null;
//            else if (AppConst.GetServerConfig("ServerState") == "1")
//            {
//                CheckCanIntoGame();
//                yield break;
//            }
//            CheckCanIntoGame();
//        }

//        public void CheckCanIntoGame()
//        {
//            //设置初次启动开关
//            bool isFirst = false;
//            if (PlayerPrefs.GetInt("isFirstCheck", 0) == 0)
//            {
//                isFirst = true;
//                PlayerPrefs.SetInt("isFirstCheck", 1);
//            }

//            if (!isFirst && AppConst.UserLanguage != "SH" && AppConst.GetServerConfig("ShowAnnounce") == "true")   //满足弹公告时执行，否则直接进入游戏
//            {
//                Debug.Log("可以弹公告啦");
//                GameStartCallBack callback = () =>
//                {
//                    // ServerState 服务器状态    0.正常  1.维护   ...后续添加
//                    switch (AppConst.GetServerConfig("ServerState"))
//                    {
//                        case "0":
//                            Debug.Log("服务器没啥毛病 ，可以进去啦");
//                            //检测完成，可以启动游戏了
//                            if (m_checkCallback != null)
//                            {
//                                WooEngine.LuaHelper.SendTimeInfoByHttp(80000);
//                                m_checkCallback(ResCheckRst.SUCCESS);
//                            }
//                            break;
//                        case "1":
//                            Debug.Log("服务器维护中，再给你弹一次");
//                            StartCoroutine(CheckServerActive());
//                            //CheckCanIntoGame();
//                            break;
//                        default:
//                            Debug.Log("默认直接进去吧");
//                            //检测完成，可以启动游戏了
//                            if (m_checkCallback != null)
//                            {
//                                WooEngine.LuaHelper.SendTimeInfoByHttp(80000);
//                                m_checkCallback(ResCheckRst.SUCCESS);
//                            }
//                            break;
//                    }
//                };
//                //弹出网页
//                if (announceObj == null)
//                {
//                    GameObject model = Resources.Load<GameObject>("AnnounceMent");
//                    if (model)
//                    {
//                        GameObject clone = GameObject.Instantiate(model);
//                        clone.transform.SetParent(GameObject.Find("UI Root").transform);
//                        clone.transform.localPosition = Vector3.zero;
//                        clone.transform.localScale = Vector3.one;
//                        RectTransform rect = clone.GetComponent<RectTransform>();
//                        rect.offsetMin = Vector2.zero;
//                        rect.offsetMax = Vector2.zero;
//                        clone.SetActive(true);

//                        clone.GetComponent<DOW.SimpleWebView>().SetGameCallBack(callback);
//                        announceObj = clone;
//                    }
//                }
//                else
//                {
//                    announceObj.SetActive(true);
//                    announceObj.GetComponent<DOW.SimpleWebView>().SetActive(true); ;
//                }
//            }
//            else
//            {
//                Debug.Log("不需要弹公告，直接进入游戏");
//                //检测完成，可以启动游戏了
//                if (m_checkCallback != null)
//                {
//                    WooEngine.LuaHelper.SendTimeInfoByHttp(80000);
//                    m_checkCallback(ResCheckRst.SUCCESS);
//                }
//            }
//        }

//        private void CheckerDebug(string log)
//        {
//            Debug.LogWarning("ResChecker:" + log);
//        }

//        private void SetLoadingActive(bool active)
//        {
//            if (showPage)
//                showPage.SetLoadingActive(active);
//        }
//    }
//}