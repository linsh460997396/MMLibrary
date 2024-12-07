using MetalMaxSystem;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MetalMaxSystem
{
    /// <summary>
    /// 纹理分析器（用于场景图片扫描分析，打印纹理编号文本，输出特征图）
    /// </summary>
    public class TextureAnalyzer : MonoBehaviour
    {
        //加MonoBehaviour的必须是实例类，可继承使用MonoBehaviour下的方法，只有继承MonoBehaviour的脚本才能被附加到游戏物体上成为其组件，并且可以使用协程和摧毁引擎对象

        //void Start()
        //{
        //    //应用示范
        //    string folderPath = "C:/Users/linsh/Desktop/地图"; //填写要扫描的文件夹
        //    string savePathFrontStr01 = "C:/Users/linsh/Desktop/MapSP/"; //输出纹理集图片的目录前缀字符
        //    string savePathFrontStr02 = "C:/Users/linsh/Desktop/MapIndex/"; //输出纹理文本的目录前缀字符
        //    StartSliceTextureAndSetSpriteIDMultiMergerAsync(folderPath, "*.png", 0.7f, 10, savePathFrontStr01, savePathFrontStr02); //仅支持png和jpg
        //}

        #region 功能函数

        /// <summary>
        /// 启动一个协程处理目标文件夹下指定后缀图片并分割成精灵，然后根据纹理像素相似度给组中精灵编号并保存配套纹理集及文本到桌面
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="searchPattern">仅支持这两种图片后缀"*.png"、"*.jpg"</param>
        /// <param name="similarity">相似度</param>
        /// <param name="handleCountMax">处理量</param>
        /// <param name="savePathFrontSP">输出纹理集图片的目录前缀字符如"C:/Users/linsh/Desktop/MapSP/"</param>
        /// <param name="savePathFrontIndex">输出纹理文本的目录前缀字符如"C:/Users/linsh/Desktop/MapIndex/"</param>
        public void StartSliceTextureAndSetSpriteIDMultiMergerAsync(string folderPath, string searchPattern, float similarity, int handleCountMax, string savePathFrontSP, string savePathFrontIndex)
        {
            StartCoroutine(SliceTextureAndSetSpriteIDMultiMergerAsync(folderPath, searchPattern, similarity, handleCountMax, savePathFrontSP, savePathFrontIndex));
        }

        /// <summary>
        /// 协程处理目标文件夹下指定后缀图片并分割成精灵，然后根据纹理像素相似度给组中精灵编号并保存配套纹理集及文本到桌面
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="searchPattern">仅支持这两种图片后缀"*.png"、"*.jpg"</param>
        /// <param name="similarity">相似度</param>
        /// <param name="handleCountMax">处理量</param>
        /// <param name="savePathFrontSP">输出纹理集图片的目录前缀字符如"C:/Users/linsh/Desktop/MapSP/"</param>
        /// <param name="savePathFrontIndex">输出纹理文本的目录前缀字符如"C:/Users/linsh/Desktop/MapIndex/"</param>
        /// <returns></returns>
        IEnumerator SliceTextureAndSetSpriteIDMultiMergerAsync(string folderPath, string searchPattern, float similarity, int handleCountMax, string savePathFrontSP, string savePathFrontIndex)
        {
            Texture2D texture; Sprite[] sprites; StringBuilder sb = new StringBuilder(); Color[] currentPixels; Color[] comparisonPixels; string fileName;
            string fileSavePath; float sim; int currentID = 1; int handleCount = 0; int jCount = 0; int fileCount = -1; int sliceMaxId = 1;
            // 存储精灵编号
            //List<int> sliceIds = new List<int>(); //换成可变列表存放
            // 存放特征精灵
            List<Sprite> spritesList = new List<Sprite>();
            Dictionary<int, string> DataTableISCP = new Dictionary<int, string>();

            // 获取文件夹下所有指定类型文件的路径 
            string[] filePaths = Directory.GetFiles(folderPath, searchPattern);

            //遍历文件夹内所有图片
            foreach (string filePath in filePaths)
            {
                fileCount++;
                sb.Clear();

                // 加载图片 
                texture = LoadImageAndConvertToTexture2D(filePath);
                // 处理图片，分割为精灵小组
                sprites = SliceTexture(texture, 16, 16);

                if (sprites == null || sprites.Length == 0)
                {
                    Debug.LogError("未找到资源");
                }
                else
                {
                    //Debug.Log("分割出 " + sprites.Length + " 个Sprite");
                    // 设定纹理文本保存路径
                    fileName = Path.GetFileNameWithoutExtension(filePath);
                    fileSavePath = savePathFrontIndex + fileName + ".txt";
                    // 新建指定长度数组存储精灵编号
                    int[] sliceIds = new int[sprites.Length + 1];

                    // 初始化切片编号数组
                    for (int ei = 0; ei < sprites.Length; ei++)
                    {
                        sliceIds[ei] = 0; // 使用0表示尚未分配切片编号
                    }
                    if (fileCount == 0)
                    {
                        sliceIds[0] = 1; // 第一个图的第一个切片编号为1
                        for (int i = 0; i < sprites.Length; i++)
                        {
                            if (i != 0)
                            {
                                if (jCount != -1)
                                {
                                    //新的一轮要比对，这里清空对比记录
                                    //Debug.Log("新一轮比对，这里清空对比记录（若有）");
                                    for (int ai = 0; ai <= jCount; ai++)
                                    {
                                        if (DataTableISCP.ContainsKey(sliceIds[ai]) && DataTableISCP[sliceIds[ai]] == "true")
                                        {
                                            DataTableISCP[sliceIds[ai]] = "";
                                            //Debug.Log("清空i=" + i + " SliceID: " + sliceIds[i] + " jCount " + jCount + " sliceIds[jCount]: " + sliceIds[jCount] + " CV[jCount]：" + MMCore.HD_ReturnIntCV(sliceIds[jCount], "Compared") + " ai: " + ai + " sliceIds[ai]：" + sliceIds[ai] + " CV[ai]：" + MMCore.HD_ReturnIntCV(sliceIds[ai], "Compared"));
                                        }
                                    }
                                    jCount = -1;
                                }

                                handleCount += 1;

                                // 提取当前精灵的像素数据
                                currentPixels = sprites[i].texture.GetPixels();

                                // 与已经编号的精灵进行对比
                                for (int j = 0; j < i; j++)
                                {
                                    //记录jCount，用于每轮清空清空标记的量
                                    jCount = j;

                                    //Debug.Log("进入i=" + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " j=" + j + " SliceJID: " + sliceIds[j] + " CV[j]：" + DataTableISCP[sliceIds[j]] + " 当前纹理最大编号：" + sliceMaxId);
                                    //读取j的编号，如已比对过该编号则跳过（提升性能，减少不必要的纹理比对），没有比对过则记录并比对一次（下次遇到该编号都跳过）
                                    if (!DataTableISCP.ContainsKey(sliceIds[j]) || DataTableISCP[sliceIds[j]] != "true")
                                    {
                                        //如果键不存在，或键存在但值不是true，那么设定为true
                                        DataTableISCP[sliceIds[j]] = "true";
                                        //Debug.Log("标记i=" + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " j=" + j + " SliceJID: " + sliceIds[j] + " CV[j]：" + DataTableISCP[sliceIds[j]] + " 当前纹理最大编号：" + sliceMaxId);
                                    }
                                    else
                                    {
                                        //Debug.Log("跳过i=" + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " j=" + j + " SliceJID: " + sliceIds[j] + " CV[j]：" + DataTableISCP[sliceIds[j]] + " 当前纹理最大编号：" + sliceMaxId);
                                        if (i == j + 1)
                                        {
                                            //这种情况是已编号切片对应类型均已比对，直接切片编号+1，以避免A型错误
                                            sliceMaxId++; sliceIds[i] = sliceMaxId;
                                            //if (sliceIds[i] == 0) 
                                            //{ 
                                            //    Debug.LogError("A型错误！Sprite " + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " j=" + j + " SliceJID: " + sliceIds[j] + " CV[j]：" + DataTableISCP[sliceIds[j]] + " 当前纹理最大编号：" + sliceMaxId);
                                            //}
                                            //Debug.Log("对比结束！jCount=" + jCount);
                                        }

                                        continue;
                                    }

                                    // 提取对比精灵的像素数据
                                    comparisonPixels = sprites[j].texture.GetPixels();

                                    // 计算相似度
                                    sim = CalculateSimilarity(currentPixels, comparisonPixels);

                                    // 如果相似度达到或以上，则分配相同的切片编号
                                    if (sim >= similarity)
                                    {
                                        sliceIds[i] = sliceIds[j];
                                        if (sliceIds[j] == 0) { Debug.LogError("B型错误！Sprite " + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " j=" + j + " SliceJID: " + sliceIds[j] + " CV[j]：" + DataTableISCP[sliceIds[j]] + " 当前纹理最大编号：" + sliceMaxId); }
                                        break;
                                    }
                                    else if (j == i - 1)
                                    {
                                        sliceMaxId++; sliceIds[i] = sliceMaxId;
                                        if (sliceIds[i] == 0) { Debug.LogError("C型错误！Sprite " + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " j=" + j + " SliceJID: " + sliceIds[j] + " CV[j]：" + DataTableISCP[sliceIds[j]] + " 当前纹理最大编号：" + sliceMaxId); }
                                    }
                                }
                            }

                            //Debug.Log("已处理Sprite " + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " jCount=" + jCount + " SliceJID: " + sliceIds[jCount] + " CV[j]：" + MMCore.HD_ReturnIntCV(sliceIds[jCount], "Compared") + " 当前纹理最大编号：" + sliceMaxId);

                            sb.Append(sliceIds[i]);

                            //开始过滤：只取特征精灵存入列表
                            if (sliceIds[i] == currentID)
                            {
                                spritesList.Add(sprites[i]);
                                currentID++;
                            }

                            // 不是最后一个整数时添加逗号
                            if (i < sprites.Length - 1)
                            {
                                sb.Append(",");
                            }
                            if (handleCount >= handleCountMax)
                            {
                                handleCount = 0;
                                //达到处理量则暂停一下协程，避免卡顿
                                yield return null;
                            }
                        }
                    }
                    else
                    {
                        //第二个图开始
                        for (int i = 0; i < sprites.Length; i++)
                        {
                            // 提取当前精灵的像素数据
                            currentPixels = sprites[i].texture.GetPixels();

                            // 与已经编号的精灵列表中的精灵进行对比
                            for (int j = 0; j < spritesList.Count; j++)
                            {
                                // 提取对比精灵的像素数据
                                comparisonPixels = spritesList[j].texture.GetPixels();

                                // 计算相似度
                                sim = CalculateSimilarity(currentPixels, comparisonPixels);

                                // 如果相似度达到或以上，则分配相同的切片编号
                                if (sim >= similarity)
                                {
                                    sliceIds[i] = j + 1; //直接娶特征纹理编号
                                    break;
                                }
                                else if (j == spritesList.Count - 1)
                                {
                                    //如果不相似但已经是最后一个特征纹理的对比
                                    sliceMaxId++; sliceIds[i] = sliceMaxId; //取最大句柄
                                }
                            }
                            sb.Append(sliceIds[i]);

                            //开始过滤：只取特征精灵存入列表
                            if (sliceIds[i] == currentID)
                            {
                                spritesList.Add(sprites[i]);
                                currentID++;
                            }

                            // 不是最后一个整数时添加逗号
                            if (i < sprites.Length - 1)
                            {
                                sb.Append(",");
                            }
                            if (handleCount >= handleCountMax)
                            {
                                handleCount = 0;
                                //达到处理量则暂停一下协程，避免卡顿
                                yield return null;
                            }
                        }
                    }
                    //将StringBuilder内容写入文件，生成纹理文本
                    MMCore.SaveFile(fileSavePath, sb.ToString());
                    Debug.Log("保存成功: " + fileSavePath);
                }
                Debug.Log("已处理图片：" + fileCount);
            }
            //生成最终纹理集
            SpriteMerger(spritesList, "MapSP", 50, savePathFrontSP);
            Debug.Log("处理完成！");
        }

        /// <summary>
        /// 启动一个协程处理目标文件夹下指定后缀图片并分割成精灵，然后根据纹理像素相似度给组中精灵编号并保存配套纹理集及文本到桌面
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="searchPattern">仅支持这两种图片后缀"*.png"、"*.jpg"</param>
        /// <param name="similarity">相似度</param>
        /// <param name="handleCountMax"></param>
        /// <param name="savePathFrontSP">输出纹理集图片的目录前缀字符如"C:/Users/linsh/Desktop/MapSP/"</param>
        /// <param name="savePathFrontIndex">输出纹理文本的目录前缀字符如"C:/Users/linsh/Desktop/MapIndex/"</param>
        public void StartSliceTextureAndSetSpriteIDAsync(string folderPath, string searchPattern, float similarity, int handleCountMax, string savePathFrontSP, string savePathFrontIndex)
        {
            StartCoroutine(SliceTextureAndSetSpriteIDAsync(folderPath, searchPattern, similarity, handleCountMax, savePathFrontSP, savePathFrontIndex));
        }

        /// <summary>
        /// 协程处理目标文件夹下指定后缀图片并分割成精灵，然后根据纹理像素相似度给组中精灵编号并保存配套纹理集及文本到桌面
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="searchPattern">仅支持这两种图片后缀"*.png"、"*.jpg"</param>
        /// <param name="similarity">相似度</param>
        /// <param name="handleCountMax"></param>
        /// <param name="savePathFrontSP">输出纹理集图片的目录前缀字符如"C:/Users/linsh/Desktop/MapSP/"</param>
        /// <param name="savePathFrontIndex">输出纹理文本的目录前缀字符如"C:/Users/linsh/Desktop/MapIndex/"</param>
        IEnumerator SliceTextureAndSetSpriteIDAsync(string folderPath, string searchPattern, float similarity, int handleCountMax, string savePathFrontSP, string savePathFrontIndex)
        {
            Texture2D texture; Sprite[] sprites; StringBuilder sb = new StringBuilder(); Color[] currentPixels; Color[] comparisonPixels; string fileName;
            string fileSavePath; float sim; int currentID; int handleCount; int jCount; int fileCount = -1; int sliceMaxId;
            // 存储精灵编号
            //List<int> sliceIds = new List<int>(); //换成可变列表存放
            // 存放特征精灵
            List<Sprite> spritesList = new List<Sprite>();
            Dictionary<int, string> DataTableISCP = new Dictionary<int, string>();

            // 获取文件夹下所有BMP文件的路径 
            string[] filePaths = Directory.GetFiles(folderPath, searchPattern);

            //遍历文件夹内所有图片
            foreach (string filePath in filePaths)
            {
                fileCount++;
                //如果图片过多，下面动作最好由多个线程进行处理

                //清空复用（new也行）
                DataTableISCP.Clear();
                spritesList.Clear();
                sb.Clear();
                //下面参数始终重置
                currentID = 1; //筛选存储特征精灵用的当前ID
                sliceMaxId = 1; //切片当前最大ID
                handleCount = 0; //重置协程处理计数
                jCount = 0; //重置jCount

                // 加载图片 
                texture = LoadImageAndConvertToTexture2D(filePath);
                // 处理图片，分割为精灵小组
                sprites = SliceTexture(texture, 16, 16);

                if (sprites == null || sprites.Length == 0)
                {
                    Debug.LogError("未找到资源");
                }
                else
                {
                    //Debug.Log("共有 " + sprites.Length + " 个Sprite");
                    // 设定纹理文本保存路径
                    fileName = Path.GetFileNameWithoutExtension(filePath);
                    fileSavePath = savePathFrontIndex + fileName + ".txt";
                    // 新建指定长度数组存储精灵编号
                    int[] sliceIds = new int[sprites.Length + 1];

                    // 初始化切片编号数组
                    for (int ei = 1; ei < sprites.Length; ei++)
                    {
                        sliceIds[ei] = 0; // 使用0表示尚未分配切片编号
                    }
                    sliceIds[0] = 1; // 第一个切片编号为1

                    //对每个精灵进行像素对比
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        if (i != 0)
                        {
                            if (jCount != -1)
                            {
                                //新的一轮要比对，这里清空对比记录
                                //Debug.Log("新一轮比对，这里清空对比记录（若有）");
                                for (int ai = 0; ai <= jCount; ai++)
                                {
                                    if (DataTableISCP.ContainsKey(sliceIds[ai]) && DataTableISCP[sliceIds[ai]] == "true")
                                    {
                                        DataTableISCP[sliceIds[ai]] = "";
                                        //Debug.Log("清空i=" + i + " SliceID: " + sliceIds[i] + " jCount " + jCount + " sliceIds[jCount]: " + sliceIds[jCount] + " CV[jCount]：" + MMCore.HD_ReturnIntCV(sliceIds[jCount], "Compared") + " ai: " + ai + " sliceIds[ai]：" + sliceIds[ai] + " CV[ai]：" + MMCore.HD_ReturnIntCV(sliceIds[ai], "Compared"));
                                    }
                                }
                                jCount = -1;
                            }

                            handleCount += 1;

                            // 提取当前精灵的像素数据
                            currentPixels = sprites[i].texture.GetPixels();

                            // 与已经编号的精灵进行对比
                            for (int j = 0; j < i; j++)
                            {
                                //记录jCount，用于每轮清空清空标记的量
                                jCount = j;

                                //Debug.Log("进入i=" + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " j=" + j + " SliceJID: " + sliceIds[j] + " CV[j]：" + DataTableISCP[sliceIds[j]] + " 当前纹理最大编号：" + sliceMaxId);
                                //读取j的编号，如已比对过该编号则跳过（提升性能，减少不必要的纹理比对），没有比对过则记录并比对一次（下次遇到该编号都跳过）
                                if (!DataTableISCP.ContainsKey(sliceIds[j]) || DataTableISCP[sliceIds[j]] != "true")
                                {
                                    //如果键不存在，或键存在但值不是true，那么设定为true
                                    DataTableISCP[sliceIds[j]] = "true";
                                    //Debug.Log("标记i=" + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " j=" + j + " SliceJID: " + sliceIds[j] + " CV[j]：" + DataTableISCP[sliceIds[j]] + " 当前纹理最大编号：" + sliceMaxId);
                                }
                                else
                                {
                                    //Debug.Log("跳过i=" + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " j=" + j + " SliceJID: " + sliceIds[j] + " CV[j]：" + DataTableISCP[sliceIds[j]] + " 当前纹理最大编号：" + sliceMaxId);
                                    if (i == j + 1)
                                    {
                                        //这种情况是已编号切片对应类型均已比对，直接切片编号+1，以避免A型错误
                                        sliceMaxId++; sliceIds[i] = sliceMaxId;
                                        //if (sliceIds[i] == 0) 
                                        //{ 
                                        //    Debug.LogError("A型错误！Sprite " + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " j=" + j + " SliceJID: " + sliceIds[j] + " CV[j]：" + DataTableISCP[sliceIds[j]] + " 当前纹理最大编号：" + sliceMaxId);
                                        //}
                                        //Debug.Log("对比结束！jCount=" + jCount);
                                    }

                                    continue;
                                }

                                // 提取对比精灵的像素数据
                                comparisonPixels = sprites[j].texture.GetPixels();

                                // 计算相似度
                                sim = CalculateSimilarity(currentPixels, comparisonPixels);

                                // 如果相似度达到或以上，则分配相同的切片编号
                                if (sim >= similarity)
                                {
                                    sliceIds[i] = sliceIds[j];
                                    if (sliceIds[j] == 0) { Debug.LogError("B型错误！Sprite " + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " j=" + j + " SliceJID: " + sliceIds[j] + " CV[j]：" + DataTableISCP[sliceIds[j]] + " 当前纹理最大编号：" + sliceMaxId); }
                                    break;
                                }
                                else if (j == i - 1)
                                {
                                    sliceMaxId++; sliceIds[i] = sliceMaxId;
                                    if (sliceIds[i] == 0) { Debug.LogError("C型错误！Sprite " + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " j=" + j + " SliceJID: " + sliceIds[j] + " CV[j]：" + DataTableISCP[sliceIds[j]] + " 当前纹理最大编号：" + sliceMaxId); }
                                }
                            }
                        }

                        //Debug.Log("已处理Sprite " + i + " SliceID: " + sliceIds[i] + " CV[i]：" + DataTableISCP[sliceIds[i]] + " jCount=" + jCount + " SliceJID: " + sliceIds[jCount] + " CV[j]：" + MMCore.HD_ReturnIntCV(sliceIds[jCount], "Compared") + " 当前纹理最大编号：" + sliceMaxId);

                        sb.Append(sliceIds[i]); //将精灵ID存入

                        //开始过滤：只取特征精灵存入列表
                        //取特征纹理添加到列表
                        if (sliceIds[i] == currentID)
                        {
                            spritesList.Add(sprites[i]);
                            currentID++;
                        }

                        // 不是最后一个整数时添加逗号
                        if (i < sprites.Length - 1)
                        {
                            sb.Append(",");
                        }
                        if (handleCount >= handleCountMax)
                        {
                            handleCount = 0;
                            //达到处理量则暂停一下协程，避免卡顿
                            yield return null;
                        }
                    }

                    //将StringBuilder内容写入文件，生成纹理文本
                    MMCore.SaveFile(fileSavePath, sb.ToString());
                    Debug.Log("保存成功: " + fileSavePath);

                    //生成纹理集
                    SpriteMerger(spritesList, fileName, 8, savePathFrontSP);
                }
                Debug.Log("已处理图片：" + fileCount);

            }
            Debug.Log("处理完成！");
        }

        /// <summary>
        /// 合成纹理集
        /// </summary>
        /// <param name="sprites"></param>
        /// <param name="fileName"></param>
        /// <param name="maxSpritesPerRow">每行最多放置的精灵数量</param>
        /// <param name="savePathFrontSP">输出纹理集图片的目录前缀字符如"C:/Users/linsh/Desktop/MapSP/"</param>
        public static void SpriteMerger(List<Sprite> sprites, string fileName, int maxSpritesPerRow, string savePathFrontSP)
        {
            int row; int col; int x; int y; int iCount; int spriteWidth; int spriteHeight; int totalRows; int totalWidth; int totalHeight;

            // 获取精灵的纹理数据
            Color[] spriteColors;

            /// <summary>
            /// Get the reference to the used Texture. If packed this will point to the atlas = ture.If not packed =false（will point to the source Sprite）.
            /// </summary>
            bool packed = false;

            // 检查是否有精灵要合并
            if (sprites.Count == 0)
            {
                Debug.LogError("No sprites to merge.");
            }
            else
            {
                iCount = 0;
                // 所有精灵的尺寸都必须相同（宽度和高度），采用第一个精灵的单位高宽（单位高宽是像素高宽除以每单位像素大小来的）
                //int spriteWidth = (int)sprites[0].bounds.gridSize.pixelX;
                //int spriteHeight = (int)sprites[0].bounds.gridSize.pixelY;
                if (packed)
                {
                    //这里是采用第一个精灵的像素高宽，因为texture属性是父级纹理，这里不能使用，要做计算
                    spriteWidth = (int)(sprites[0].bounds.size.x * sprites[0].pixelsPerUnit);
                    spriteHeight = (int)(sprites[0].bounds.size.y * sprites[0].pixelsPerUnit);
                }
                else
                {
                    //这里是采用第一个精灵的像素高宽
                    spriteWidth = sprites[0].texture.width;
                    spriteHeight = sprites[0].texture.height;
                }
                //Debug.Log("spriteWidth：" + spriteWidth + "spriteHeight：" + spriteHeight);

                // 计算大图的尺寸
                totalRows = (sprites.Count + maxSpritesPerRow - 1) / maxSpritesPerRow; // 向上取整得所需行数
                totalWidth = maxSpritesPerRow * spriteWidth;
                totalHeight = totalRows * spriteHeight;
                //Debug.Log("totalRows：" + totalRows + "totalWidth：" + totalWidth + "totalHeight：" + totalHeight);

                // 创建一个新的Texture2D来保存合并后的精灵
                Texture2D mergedTexture = new Texture2D(totalWidth, totalHeight, TextureFormat.RGBA32, false);

                // 合并精灵
                for (int i = 0; i < sprites.Count; i++)
                {
                    iCount++;

                    // 计算精灵在大图上的位置
                    row = i / maxSpritesPerRow;
                    col = i % maxSpritesPerRow;
                    x = col * spriteWidth;
                    y = row * spriteHeight;

                    // 获取精灵的纹理数据
                    spriteColors = sprites[i].texture.GetPixels();

                    // 将精灵绘制到合并纹理上
                    for (int spriteY = 0; spriteY < spriteHeight; spriteY++)
                    {
                        for (int spriteX = 0; spriteX < spriteWidth; spriteX++)
                        {
                            mergedTexture.SetPixel(x + spriteX, y + spriteY, spriteColors[spriteY * spriteWidth + spriteX]);
                        }
                    }

                    if (iCount >= 10000)
                    {
                        //达到协程处理量则中断，输出日志
                        iCount = 0;
                        //Debug.Log("Index：" + i);
                    }
                }

                // 应用更改到纹理
                mergedTexture.Apply();

                // 将Texture2D保存为PNG文件
                byte[] pngData = mergedTexture.EncodeToPNG();
                string savePath = savePathFrontSP + fileName + ".png";
                MMCore.SaveFile(savePath, pngData);

                //清理临时图片
                //Destroy(mergedTexture);

                // 打印消息以确认保存
                Debug.Log("Merged sprites saved to: " + savePath);
            }
        }

        /// <summary>
        /// LoadImageAndConvertToTexture2D
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Texture2D LoadImageAndConvertToTexture2D(string filePath)
        {
            byte[] fileData = File.ReadAllBytes(filePath); //打包后，路径不对的话会卡在这里
            Texture2D texture = new Texture2D(2, 2); //随便定义初始尺寸但不可以为null
            bool success = texture.LoadImage(fileData); //加载图片Unity会自动调整尺寸
            if (success)
            {
                // 图片加载成功
                //Debug.Log("Image loaded successfully with width: " + texture.width + " and height: " + texture.height);
                //Main_MMWorld.label_headTip.GetComponent<TextMeshProUGUI>().text = "Image loaded successfully with width: " + texture.width + " and height: " + texture.height;
            }
            else
            {
                // 图片加载失败，可能需要检查字节数组是否有效或图片格式是否支持
                Debug.LogError("Failed to load image.");
            }
            return texture;
        }

        /// <summary>
        /// 比对获取2个纹理的像素颜色相似度
        /// </summary>
        /// <param name="pixels1"></param>
        /// <param name="pixels2"></param>
        /// <returns></returns>
        public static float CalculateSimilarity(Color[] pixels1, Color[] pixels2)
        {
            if (pixels1.Length != pixels2.Length)
                return 0f;

            int differentPixelCount = 0;
            for (int i = 0; i < pixels1.Length; i++)
            {
                if (pixels1[i] != pixels2[i])
                {
                    differentPixelCount++;
                }
            }

            return 1f - (float)differentPixelCount / pixels1.Length;
        }

        /// <summary>
        /// 将纹理切割成多个切片，并返回包含这些切片的Sprite数组
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="width">像素宽</param>
        /// <param name="height">像素高</param>
        /// <returns></returns>
        public static Sprite[] SliceTexture(Texture2D texture, int width, int height)
        {
            // 计算切片数量
            int numSlicesX = texture.width / width;
            int numSlicesY = texture.height / height;
            int totalSlices = numSlicesX * numSlicesY;
            Debug.Log("SliceTexture：texture.width " + texture.width + " texture.height " + texture.height + " totalSlices " + totalSlices);

            // 创建一个数组来存储切片
            Sprite[] slices = new Sprite[totalSlices];
            int sliceIndex = 0;

            // 遍历纹理的每个切片
            for (int y = 0; y < numSlicesY; y++)
            {
                for (int x = 0; x < numSlicesX; x++)
                {
                    // 创建一个新的纹理来存储切片
                    Texture2D sliceTexture = new Texture2D(width, height);

                    // 复制像素到新的纹理切片中
                    for (int py = 0; py < height; py++)
                    {
                        for (int px = 0; px < width; px++)
                        {
                            sliceTexture.SetPixel(px, py, texture.GetPixel(x * width + px, y * height + py));
                        }
                    }

                    // 应用更改到纹理
                    sliceTexture.Apply();

                    // 创建一个新的Sprite，并将其纹理设置为切片纹理
                    Sprite sliceSprite = Sprite.Create(sliceTexture, new Rect(0, 0, sliceTexture.width, sliceTexture.height), new Vector2(0.5f, 0.5f));

                    // 将切片Sprite添加到数组中
                    slices[sliceIndex++] = sliceSprite;
                }
            }

            // 返回切片数组
            return slices;
        }

        #endregion
    }
}