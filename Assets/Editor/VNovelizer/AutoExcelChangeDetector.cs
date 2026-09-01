using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Excel 剧本自动转换监听器。创建日期：2026-08-30。
/// </summary>
public class AutoExcelChangeDetector : AssetPostprocessor
{
    private const string StatusMessageKey = "VNovelizer.AutoExcelConversion.Status";
    private const string StatusIsErrorKey = "VNovelizer.AutoExcelConversion.IsError";
    private const string StatusVersionKey = "VNovelizer.AutoExcelConversion.Version";

    // 更新日期：2026-08-31。记录单个文件失败次数，避免无限重试。
    private static readonly Dictionary<string, int> retryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> pendingFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private const double ConvertDelaySeconds = 0.5d;
    private const double RetryDelaySeconds = 1.0d;
    private const int MaxRetryCount = 2;
    private static double processTime;
    private static bool processScheduled;

    private static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        VNProjectConfig config = VNProjectConfig.Instance;
        if (config == null || !config.AutoConvertExcel) 
            return;

        string excelFolder = NormalizePath(config.GetExcelFolderPath());
        if (string.IsNullOrEmpty(excelFolder)) 
            return;

        excelFolder = excelFolder.TrimEnd('/'); // TrimEnd：移出字符串末尾的特定字符

        foreach (string assetPath in importedAssets)
        {
            string normalizedPath = NormalizePath(assetPath);
            if (!normalizedPath.StartsWith(excelFolder + "/", StringComparison.OrdinalIgnoreCase)) 
                continue;

            string extension = Path.GetExtension(normalizedPath);
            if (!extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".xls", StringComparison.OrdinalIgnoreCase)) continue;

            string fileName = Path.GetFileName(normalizedPath);
            // 忽略 WPS 保存时产生的 ~$Chapter1.xlsx 临时文件。
            if (fileName.StartsWith("~$", StringComparison.OrdinalIgnoreCase)) 
                continue;

            pendingFiles.Add(normalizedPath);
            Debug.Log($"检测到剧本 Excel 发生变化，准备自动转换：{normalizedPath}");
        }

        ScheduleProcess();
    }

    // 延迟并合并重复事件，避免 WPS 一次保存触发多次转换。
    private static void ScheduleProcess(double delaySeconds = ConvertDelaySeconds)
    {
        // EditorApplication.timeSinceStartup：获取 Unity 编辑器自打开以来流逝的时间
        processTime = EditorApplication.timeSinceStartup + delaySeconds;
        if (processScheduled) 
            return;

        processScheduled = true;
        // Unity Editor 的 update 事件会在每一帧调用，注册一个回调函数（每一帧都会执行），当时间到达时执行处理
        EditorApplication.update += ProcessWhenReady;
    }

    private static void ProcessWhenReady()
    {
        if (EditorApplication.timeSinceStartup < processTime) 
            return;

        EditorApplication.update -= ProcessWhenReady;
        processScheduled = false;
        ProcessPendingFiles();
    }

    private static void ProcessPendingFiles()
    {
        if (pendingFiles.Count == 0) 
            return;

        VNProjectConfig config = VNProjectConfig.Instance;
        string csvOutputFolder = config != null ? config.GetCsvOutputPath() : "";
        if (string.IsNullOrEmpty(csvOutputFolder))
        {
            Debug.LogError("CSV 输出文件夹未配置，无法自动转换剧本。");
            PublishStatus("自动转换失败：CSV 输出文件夹未配置。", true);
            pendingFiles.Clear();
            return;
        }

        string[] files = new string[pendingFiles.Count];
        pendingFiles.CopyTo(files); // 将待处理的文件路径复制到数组中，避免在迭代时修改集合
        pendingFiles.Clear();
        int successCount = 0;

        foreach (string assetPath in files)
        {
            if (!File.Exists(assetPath)) 
                continue;

            try
            {
                // 确认文件已经可以读取，降低 WPS 尚未完成写入导致失败的概率。
                using (FileStream stream = File.Open(assetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) { }
                ExcelToCsvConverter.ConvertFile(assetPath, csvOutputFolder);
                retryCounts.Remove(assetPath);
                successCount++;
                Debug.Log($"剧本自动转换完成：{assetPath}");
            }
            catch (Exception exception)
            {
                HandleConversionFailure(assetPath, exception);
                // 转换失败时保留原有 CSV，不覆盖上一份可用数据。
                Debug.LogError($"剧本自动转换失败：{assetPath}\n{exception.Message}");
            }
        }

        if (successCount > 0)
        {
            AssetDatabase.Refresh();
            // 发布剧本转换完成的状态信息，之后在剧本管理器（ScriptManager）中可以读取到这个状态信息，并显示给用户
            PublishStatus($"自动转换完成，共处理 {successCount} 个剧本。", false);
            Debug.Log($"自动转换完成，共处理 {successCount} 个剧本。");
        }
    }

    /// <summary>
    /// 转换失败时最多重试两次。每次等待一秒，以应对 WPS 延迟释放文件的情况。
    /// </summary>
    private static void HandleConversionFailure(string assetPath, Exception exception)
    {
        int retryCount = retryCounts.TryGetValue(assetPath, out int count) ? count + 1 : 1;

        if (retryCount > MaxRetryCount)
        {
            retryCounts.Remove(assetPath);
            PublishStatus($"自动转换失败：{Path.GetFileName(assetPath)}", true);
            Debug.LogError($"剧本自动转换已重试 {MaxRetryCount} 次，仍然失败：{assetPath}\n{exception.Message}");
            return;
        }

        retryCounts[assetPath] = retryCount;
        pendingFiles.Add(assetPath);
        ScheduleProcess(RetryDelaySeconds);
        PublishStatus($"自动转换暂时失败，正在重试：{Path.GetFileName(assetPath)}", true);
        Debug.LogWarning($"剧本自动转换失败，将在 1 秒后重试（{retryCount}/{MaxRetryCount}）：{assetPath}");
    }

    // [2026-8-31] 新增
    // 通过 SessionState 向 VNovelizer.Editor 程序集传递最近一次转换结果。
    private static void PublishStatus(string message, bool isError)
    {
        SessionState.SetString(StatusMessageKey, message);
        SessionState.SetBool(StatusIsErrorKey, isError);
        int version = SessionState.GetInt(StatusVersionKey, 0);
        SessionState.SetInt(StatusVersionKey, version + 1);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimEnd('/');
    }
}
