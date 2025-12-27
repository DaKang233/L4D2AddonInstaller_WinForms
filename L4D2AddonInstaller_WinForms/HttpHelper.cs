using System;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace L4D2AddonInstaller_WinForms
{
    /// <summary>
    /// HTTPS请求工具类（处理ZeroSSL证书，获取远程文件）
    /// </summary>
    public static class HttpHelper
    {
        // 全局HttpClient（避免频繁创建释放）
        private static readonly HttpClient _httpClient;

        static HttpHelper()
        {
            // 配置HttpClient：忽略证书验证（若ZeroSSL证书在客户端信任则可注释）
            /*
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                {
                    // 生产环境建议验证证书指纹，而非直接返回true
                    // 示例：return cert.Thumbprint == "你的ZeroSSL证书指纹";
                    return true;
                }
            };
            */
            _httpClient = new HttpClient(/*handler*/)
            {
                Timeout = TimeSpan.FromMinutes(5) // 下载超时时间
            };
        }

        /// <summary>
        /// 异步获取远程文本文件（如download.txt）
        /// </summary>
        public static async Task<string> GetRemoteTextAsync(string url)
        {
            try
            {
                using (var response = await _httpClient.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode(); // 抛出HTTP错误（4xx/5xx）
                    return await response.Content.ReadAsStringAsync(); // 获取文本内容
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"网络请求失败：{ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"请求超时：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 异步下载文件到指定路径（带进度回调）
        /// </summary>
        /// <param name="url">文件的URL地址</param>
        /// <param name="savePath">文件的保存路径</param>
        /// <param name="progress">进度回调接口，用于报告下载进度百分比</param>
        public static async Task DownloadFileAsync(
            string url,
            string savePath,
            CancellationToken cancellationToken,
            IProgress<int> progress = null)
        {
            HttpResponseMessage response = null;
            CancellationTokenRegistration ctr = default;

            try
            {
                response = await _httpClient.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead
                );

                response.EnsureSuccessStatusCode();

                // 注册取消回调：一旦取消，立刻 Dispose response
                ctr = cancellationToken.Register(() =>
                {
                    try { response.Dispose(); } catch { }
                });

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var downloadedBytes = 0L;
                var buffer = new byte[8192];

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(savePath))
                {
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        // 主动检查取消
                        cancellationToken.ThrowIfCancellationRequested();

                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;

                        if (totalBytes > 0 && progress != null)
                        {
                            int percent = (int)(downloadedBytes * 100.0 / totalBytes);
                            progress.Report(percent);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (File.Exists(savePath))
                    File.Delete(savePath);

                throw; // 非常重要：向上传递“取消”
            }
            catch (ObjectDisposedException)
            {
                // HttpResponse 被 Dispose，通常意味着取消
                if (File.Exists(savePath))
                    File.Delete(savePath);

                throw new OperationCanceledException(cancellationToken);
            }
            catch (Exception ex)
            {
                if (File.Exists(savePath))
                    File.Delete(savePath);

                throw new Exception($"下载文件失败：{ex.Message}", ex);
            }
            finally
            {
                ctr.Dispose();
                response?.Dispose();
            }
        }

    }
}
