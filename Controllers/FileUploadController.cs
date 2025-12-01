using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FileUploadService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly string _uploadPath;
        private readonly long _maxFileSizeBytes;
        private readonly string[] _allowedContentTypes;

        // 通过配置注入上传参数
        public FileUploadController(IConfiguration config)
        {
            // 从配置读取上传路径
            var uploadDir = config.GetValue<string>("UploadSettings:UploadPath");
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), uploadDir);

            // 转换最大文件大小为字节
            var maxSizeMB = config.GetValue<int>("UploadSettings:MaxFileSizeMB");
            _maxFileSizeBytes = maxSizeMB * 1024 * 1024;

            // 读取允许的文件类型
            _allowedContentTypes = config.GetSection("UploadSettings:AllowedContentTypes")
                                         .Get<string[]>() ?? Array.Empty<string>();

            // 确保上传目录存在
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        /// <summary>
        /// 单文件上传接口
        /// </summary>
        [HttpPost("single")]
        public async Task<IActionResult> UploadSingleFile(IFormFile file)
        {
            try
            {
                // 验证文件是否存在
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { Success = false, Message = "未选择文件或文件为空" });
                }

                // 验证文件大小
                if (file.Length > _maxFileSizeBytes)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"文件大小不能超过{_maxFileSizeBytes / 1024 / 1024}MB"
                    });
                }

                // 验证文件类型
                if (!Array.Exists(_allowedContentTypes, type => type == file.ContentType))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "不支持的文件类型",
                        AllowedTypes = _allowedContentTypes
                    });
                }

                // 生成唯一文件名（避免覆盖）
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(_uploadPath, fileName);

                // 保存文件
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new
                {
                    Success = true,
                    Message = "文件上传成功",
                    OriginalFileName = file.FileName,
                    StoredFileName = fileName,
                    FileSizeBytes = file.Length,
                    FilePath = filePath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Success = false, Message = $"上传失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 多文件上传接口
        /// </summary>
        [HttpPost("multiple")]
        public async Task<IActionResult> UploadMultipleFiles(IFormFileCollection files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { Success = false, Message = "未选择任何文件" });
            }

            var result = new List<object>();

            foreach (var file in files)
            {
                try
                {
                    // 验证文件是否为空
                    if (file.Length == 0)
                    {
                        result.Add(new
                        {
                            FileName = file.FileName,
                            Success = false,
                            Message = "文件为空"
                        });
                        continue;
                    }

                    // 验证文件大小
                    if (file.Length > _maxFileSizeBytes)
                    {
                        result.Add(new
                        {
                            FileName = file.FileName,
                            Success = false,
                            Message = $"文件大小超过限制（{_maxFileSizeBytes / 1024 / 1024}MB）"
                        });
                        continue;
                    }

                    // 生成唯一文件名并保存
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                    var filePath = Path.Combine(_uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    result.Add(new
                    {
                        Success = true,
                        OriginalFileName = file.FileName,
                        StoredFileName = fileName,
                        FileSizeBytes = file.Length
                    });
                }
                catch (Exception ex)
                {
                    result.Add(new
                    {
                        FileName = file.FileName,
                        Success = false,
                        Message = ex.Message
                    });
                }
            }

            return Ok(result);
        }
    }
}